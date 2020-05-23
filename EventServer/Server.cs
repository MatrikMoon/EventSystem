using EventServer.Database;
using EventServer.Discord;
using EventShared;
using EventShared.SimpleJSON;
using SimpleHttpServer;
using SimpleHttpServer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using static EventServer.Database.SqlUtils;
using static EventShared.SharedConstructs;
using Score = EventShared.Score;

namespace EventServer
{
    class Server
    {
        public static void StartHttpServer()
        {
            var route_config = new List<Route>() {
                new Route {
                    Name = "Score Receiver",
                    UrlRegex = @"^/submit/$",
                    Method = "POST",
                    Callable = (HttpRequest request) => {
                        try
                        {
                            //Get JSON object from request content
                            JSONNode node = JSON.Parse(WebUtility.UrlDecode(request.Content));

                            //Get Score object from JSON
                            Score s = Score.FromString(node["pb"]);

                            if (RSA.SignScore(Convert.ToUInt64(s.UserId), s.SongHash, s.Difficulty, s.Characteristic, s.FullCombo, s.Score_, s.PlayerOptions, s.GameOptions) == s.Signed &&
                                Song.Exists(s.SongHash, (LevelDifficulty)s.Difficulty, s.Characteristic, true) &&
                                !new Song(s.SongHash, (LevelDifficulty)s.Difficulty, s.Characteristic).Old &&
                                Player.Exists(s.UserId) &&
                                Player.IsRegistered(s.UserId))
                            {
                                Logger.Info($"RECEIVED VALID SCORE: {s.Score_} FOR {new Player(s.UserId).DiscordName} {s.SongHash} {s.Difficulty} {s.Characteristic}");
                            }
                            else
                            {
                                Logger.Error($"RECEIVED INVALID SCORE {s.Score_} FROM {new Player(s.UserId).DiscordName} FOR {s.UserId} {s.SongHash} {s.Difficulty} {s.Characteristic}");
                                return new HttpResponse()
                                {
                                    ReasonPhrase = "Bad Request",
                                    StatusCode = "400"
                                };
                            }

                            Database.Score oldScore = null;
                            if (Database.Score.Exists(s.SongHash, s.UserId, (LevelDifficulty)s.Difficulty, s.Characteristic))
                            {
                                oldScore = new Database.Score(s.SongHash, s.UserId, (LevelDifficulty)s.Difficulty, s.Characteristic);
                            }

                            if (oldScore == null ^ (oldScore != null && oldScore.GetScore() < s.Score_))
                            {
                                Player player = new Player(s.UserId);

                                long oldScoreNumber = oldScore == null ? 0 : oldScore.GetScore();
                                oldScore?.SetOld();

                                //Player stats
                                if (oldScoreNumber > 0) player.IncrementPersonalBestsBeaten();
                                else player.IncrementSongsPlayed();
                                player.IncrementSongsPlayed();
                                player.TotalScore += s.Score_ - oldScoreNumber; //Increment total score only by the amount the score has increased

                                Database.Score newScore = new Database.Score(s.SongHash, s.UserId, (LevelDifficulty)s.Difficulty, s.Characteristic);
                                newScore.SetScore(s.Score_, s.FullCombo);

                                //Only send message if player is registered
                                if (Player.Exists(s.UserId)) CommunityBot.SendToScoreChannel($"User \"{player.DiscordMention}\" has scored {s.Score_} on {new Song(s.SongHash, (LevelDifficulty)s.Difficulty, s.Characteristic).SongName} ({(LevelDifficulty)s.Difficulty}) ({s.Characteristic})!");
                            }

                            return new HttpResponse()
                            {
                                ReasonPhrase = "OK",
                                StatusCode = "200"
                            };
                        }
                        catch (Exception e)
                        {
                            Logger.Error($"{e}");
                        }

                        return new HttpResponse()
                        {
                            ReasonPhrase = "Bad Request",
                            StatusCode = "400"
                        };
                     }
                },
                new Route {
                    Name = "Rank Receiver",
                    UrlRegex = @"^/requestrank/$",
                    Method = "POST",
                    Callable = (HttpRequest request) => {
                        try
                        {
                            //Get JSON object from request content
                            JSONNode node = JSON.Parse(WebUtility.UrlDecode(request.Content));

                            //Get Score object from JSON
                            RankRequest r = RankRequest.FromString(node["pb"]);

                            if (RSA.SignRankRequest(Convert.ToUInt64(r.UserId), r.RequestedTeamId, r.InitialAssignment) == r.Signed &&
                                Player.Exists(r.UserId) &&
                                Player.IsRegistered(r.UserId) &&
                                Team.Exists(r.RequestedTeamId))
                            {
                                Logger.Info($"RECEIVED VALID RANK REQUEST: {r.RequestedTeamId} FOR {r.UserId} {r.RequestedTeamId} {r.InitialAssignment}");

                                var player = new Player(r.UserId);
                                var team = new Team(r.RequestedTeamId);

                                //The rank up system will ignore requets where the player doesn't have the required tokens,
                                //or is requesting a rank higher than the one above their current rank (if it's not an inital rank assignment)
                                if (r.InitialAssignment && player.Team == "-1") CommunityBot.ChangeTeam(player, team);
                                else if (player.Team != "gold") {
                                    var oldTeam = new Team(player.Team);
                                    var nextTeam = new Team(oldTeam.NextPromotion);
                                    if (player.Tokens >= nextTeam.RequiredTokens) CommunityBot.ChangeTeam(player, nextTeam);
                                }
                                else if (player.Team == "gold" && player.Tokens >= new Team("blue").RequiredTokens) { //Player is submitting for Blue
                                    new Vote(player.UserId, r.OstScoreInfo);
                                }
                                else
                                {
                                    return new HttpResponse()
                                    {
                                        ReasonPhrase = "Bad Request",
                                        StatusCode = "400"
                                    };
                                }

                                return new HttpResponse()
                                {
                                    ReasonPhrase = "OK",
                                    StatusCode = "200"
                                };
                            }
                            else
                            {
                                Logger.Warning($"RECEIVED INVALID RANK REQUEST {r.RequestedTeamId} FROM {r.UserId}");
                            }
                        }
                        catch (Exception e)
                        {
                            Logger.Error($"{e}");
                        }

                        return new HttpResponse()
                        {
                            ReasonPhrase = "Bad Request",
                            StatusCode = "400"
                        };
                    }
                },
                new Route {
                    Name = "Song Getter",
                    UrlRegex = @"^/songs/",
                    Method = "GET",
                    Callable = (HttpRequest request) => {
                        string[] requestData = request.Path.Substring(1).Split('/');
                        string userId = requestData[1];
                        userId = Regex.Replace(userId, "[^a-zA-Z0-9- ]", "");

                        //If the userid is null, we'll give them e+ everything. It's likely the leaderboard site
                        if (!string.IsNullOrEmpty(userId) && (!Player.Exists(userId) || !Player.IsRegistered(userId)))
                        {
                            return new HttpResponse()
                            {
                                ReasonPhrase = "Bad Request",
                                StatusCode = "400"
                            };
                        }

                        JSONNode json = new JSONObject();
                        List<SongConstruct> songs = GetActiveSongs();

                        songs.ForEach(x => {
                            //If the request doesn't include a player id, we can't be sure which difficulty the player is supposed to play, so we'll just leave
                            //the difficulty set to Auto because it's probably the website asking
                            if (x.Difficulty == LevelDifficulty.Auto && !string.IsNullOrEmpty(userId)) {
                                if (OstHelper.IsOst(x.SongHash))
                                {
                                    x.Difficulty = new Player(userId).GetPreferredDifficulty(OstHelper.IsOst(x.SongHash));
                                }
                                else {
                                    var preferredDifficulty = new Player(userId).GetPreferredDifficulty(OstHelper.IsOst(x.SongHash));
                                    x.Difficulty = new BeatSaver.Song(x.SongHash).GetClosestDifficultyPreferLower(preferredDifficulty);
                                }
                            }

                            var item = new JSONObject();
                            item["songName"] = x.Name;
                            item["songHash"] = x.SongHash;
                            item["difficulty"] = (int)x.Difficulty;
                            item["gameOptions"] = (int)x.GameOptions;
                            item["playerOptions"] = (int)x.PlayerOptions;
                            item["characteristic"] = x.Characteristic;
                            json.Add(x.SongHash + (int)x.Difficulty, item);
                        });

                        return new HttpResponse()
                        {
                            ContentAsUTF8 = json.ToString(),
                            ReasonPhrase = "OK",
                            StatusCode = "200"
                        };
                     }
                },
                new Route {
                    Name = "Team Getter",
                    UrlRegex = @"^/teams/",
                    Method = "GET",
                    Callable = (HttpRequest request) => {
                        string source = request.Path.Substring(request.Path.LastIndexOf("/") + 1);
                        if (!string.IsNullOrEmpty(source)) Logger.Success("Team Request from Duo's Site");

                        JSONNode json = new JSONObject();
                        List<Team> teams = GetAllTeams();

                        foreach (var team in teams)
                        {
                            var item = new JSONObject();
                            item["captainId"] = team.Captain;
                            item["teamName"] = team.TeamName;
                            item["color"] = team.Color;
                            item["requiredTokens"] = team.RequiredTokens;
                            item["nextPromotion"] = team.NextPromotion;
                            json.Add(team.TeamId, item);
                        }

                        return new HttpResponse()
                        {
                            ContentAsUTF8 = json.ToString(),
                            ReasonPhrase = "OK",
                            StatusCode = "200"
                        };
                     }
                },
                new Route {
                    Name = "Player Stats Getter",
                    UrlRegex = @"^/playerstats/",
                    Method = "GET",
                    Callable = (HttpRequest request) => {
                        string userId = request.Path.Substring(request.Path.LastIndexOf("/") + 1);
                        userId = Regex.Replace(userId, "[^a-zA-Z0-9]", "");

                        if (!(userId.Length == 17 || userId.Length == 16 || userId.Length == 15)) //17 = vive, 16 = oculus, 15 = sfkwww
                        {
                            Logger.Error($"Invalid id size: {userId.Length}");
                            return new HttpResponse()
                            {
                                ReasonPhrase = "Bad Request",
                                StatusCode = "400"
                            };
                        }

                        JSONNode json = new JSONObject();

                        if (Player.Exists(userId) && Player.IsRegistered(userId)) {
                            if (Config.ServerFlags.HasFlag(ServerFlags.Teams) && new Player(userId).Team == "-1")
                            {
                                json["message"] = "Please be sure you're assigned to a team before playing";
                            }
                            else
                            {
                                Player player = new Player(userId);
                                json["version"] = VersionCode;
                                json["team"] = player.Team;
                                json["tokens"] = player.Tokens;
                                json["serverSettings"] = (int)Config.ServerFlags;
                            }
                        }
                        else if (Player.Exists(userId) && !Player.IsRegistered(userId))
                        {
                            json["message"] = "Your have been banned from this event.";
                        }
                        else
                        {
                            json["message"] = "Please register with the bot before playing. Check #plugin-information for more info. Sorry for the inconvenience.";
                        }

                        return new HttpResponse()
                        {
                            ContentAsUTF8 = json.ToString(),
                            ReasonPhrase = "OK",
                            StatusCode = "200"
                        };
                     }
                },
                new Route {
                    Name = "Song Leaderboard Getter",
                    UrlRegex = @"^/leaderboards/",
                    Method = "GET",
                    Callable = (HttpRequest request) => {
                        string[] requestData = request.Path.Substring(1).Split('/');
                        string songHash = requestData[1];
                        JSONNode json = new JSONObject();

                        if (songHash == "all")
                        {
                            int take = 10;
                            string teamId = "-1";

                            if (requestData.Length > 2) take = Convert.ToInt32(requestData[2]);
                            if (requestData.Length > 3) teamId = requestData[3];

                            List<SongConstruct> songs = GetAllScores(teamId);
                            songs.ToList().ForEach(x =>
                            {
                                JSONNode songNode = new JSONObject();
                                songNode["levelId"] = x.SongHash;
                                songNode["songName"] = x.Name;
                                songNode["difficulty"] = (int)x.Difficulty;
                                songNode["characteristic"] = x.Characteristic;
                                songNode["scores"] = new JSONObject();

                                int place = 1;
                                x.Scores.Take(take).ToList().ForEach(y =>
                                {
                                    JSONNode scoreNode = new JSONObject();
                                    scoreNode["score"] = y.Score;
                                    scoreNode["player"] = new Player(y.UserId).DiscordName;
                                    scoreNode["place"] = place;
                                    scoreNode["fullCombo"] = y.FullCombo ? "true" : "false";
                                    scoreNode["userId"] = y.UserId;
                                    scoreNode["team"] = y.TeamId;
                                    songNode["scores"].Add(Convert.ToString(place++), scoreNode);
                                });

                                json.Add(x.SongHash + ":" + (int)x.Difficulty, songNode);
                            });
                        }
                        else
                        {
                            int take = 10;
                            int difficulty = Convert.ToInt32(requestData[2]);
                            string characteristic = requestData[3];
                            string teamId = requestData[4];
                            if (requestData.Length > 5) take = Convert.ToInt32(requestData[5]);

                            songHash = Regex.Replace(songHash, "[^a-zA-Z0-9- ]", "");
                            teamId = Regex.Replace(teamId, "[^a-zA-Z0-9- ]", "");

                            SongConstruct songConstruct = new SongConstruct()
                            {
                                SongHash = songHash,
                                Difficulty = (LevelDifficulty)difficulty,
                                Characteristic = characteristic
                            };

                            if (!Song.Exists(songHash, (LevelDifficulty)difficulty, characteristic, true))
                            {
                                Logger.Error($"Song doesn't exist for leaderboards: {songHash}");
                                return new HttpResponse()
                                {
                                    ReasonPhrase = "Bad Request",
                                    StatusCode = "400"
                                };
                            }

                            List<ScoreConstruct> scores = GetScoresForSong(songConstruct, teamId);

                            int place = 1;
                            scores.Take(take).ToList().ForEach(x =>
                            {
                                JSONNode node = new JSONObject();
                                node["score"] = x.Score;
                                node["player"] = new Player(x.UserId).DiscordName;
                                node["place"] = place;
                                node["fullCombo"] = x.FullCombo ? "true" : "false";
                                node["userId"] = x.UserId;
                                node["team"] = x.TeamId;
                                json.Add(Convert.ToString(place++), node);
                            });
                        }

                        return new HttpResponse()
                        {
                            ContentAsUTF8 = json.ToString(),
                            ReasonPhrase = "OK",
                            StatusCode = "200"
                        };
                    }
                }
            };

            Logger.Info($"HTTP Server listening on {Dns.GetHostName()}");

#if (TEAMSABER)
            int port = 3707;
#elif (DISCORDCOMMUNITY)
            int port = 3703;
#elif (TRUEACCURACY)
            int port = 3711;
#elif (ASIAVR)
            int port = 3709;
#elif QUALIFIER
            int port = 3713;
#elif (BTH)
            int port = 3715;
#else
            int port = 3704; //My vhost is set up to direct to 3708 when the /api-beta/ route is followed
#endif
            HttpServer httpServer = new HttpServer(port, route_config);
            httpServer.Listen();
        }

        public static int Main(string[] args)
        {
            //Load server config
            Config.LoadConfig();

            //Start Discord bot
#if (TEAMSABER)
            var serverName = "Team Saber";
#elif (DISCORDCOMMUNITY)
            var serverName = "Beat Saber Discord Server";
#elif (TRUEACCURACY)
            var serverName = "True Accuracy Championship";
#elif (ASIAVR)
            var serverName = "Asia VR Community";
#elif QUALIFIER
            var serverName = "Beat Saber World Cup";
#elif BTH
            var serverName = "Beat the Hub Season 1";
#elif BETA
            var serverName = "Beat Saber Testing Server";
#endif

#if (TEAMSABER)
            string scoreChannel = "event-feed";
#elif (DISCORDCOMMUNITY)
            ulong scoreChannel = 457952124307898368; //"event-scores";
#elif (TRUEACCURACY)
            ulong scoreChannel = 663160463017639971; //"qualifier-feed";
#elif (ASIAVR)
            ulong scoreChannel = 572908789699969054; //"scores feed";
#elif QUALIFIER
            ulong scoreChannel = 711326932901429310; //"map-pooling-scores";
#elif BTH
            ulong scoreChannel = 707296432712843276; //"voice-text"
#elif BETA
            ulong scoreChannel = 488445468141944842; //"event-scores";
#endif


#if (TEAMSABER)
            string infoChannel = "event-feed";
#elif (DISCORDCOMMUNITY)
            ulong infoChannel = 457952124307898368; //"event-scores";
#elif (TRUEACCURACY)
            ulong infoChannel = 663160463017639971; //"qualifier-feed";
#elif (ASIAVR)
            ulong infoChannel = 572908789699969054; //"scores feed";
#elif QUALIFIER
            ulong infoChannel = 709154896183820349; //"country-registration";
#elif BTH
            ulong infoChannel = 707296432712843276; //"voice-text"
#elif BETA
            ulong infoChannel = 488445468141944842; //"event-scores";
#endif
            Thread thread1 = new Thread(async () => {
                CommunityBot.Start(serverName, scoreChannel, infoChannel);
                Vote.RegisterVotesWithBot();
                await Task.Delay(-1);
            });
            thread1.Start();

            //Set up HTTP server
            Thread thread2 = new Thread(new ThreadStart(StartHttpServer));
            thread2.Start();

            //Kill on enter press
            Console.ReadLine();
            return 0;
        }
    }
}
