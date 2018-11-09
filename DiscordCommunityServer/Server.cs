using DiscordCommunityShared;
using DiscordCommunityShared.SimpleJSON;
using SimpleHttpServer;
using SimpleHttpServer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using static DiscordCommunityServer.Database.SimpleSql;
using static DiscordCommunityShared.SharedConstructs;

namespace DiscordCommunityServer
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
                            Score s = Score.Parser.ParseFrom(Convert.FromBase64String(node["pb"]));

                            if (RSA.SignScore(Convert.ToUInt64(s.SteamId), s.SongId, s.DifficultyLevel, s.GameplayMode, s.FullCombo, s.Score_) == s.Signed &&
                                Database.Song.Exists(s.SongId, s.GameplayMode) &&
                                !new Database.Song(s.SongId, s.GameplayMode).IsOld() &&
                                Database.Player.Exists(s.SteamId) &&
                                Database.Player.IsRegistered(s.SteamId) &&
                                new BeatSaver.Song(s.SongId)
                                    .GetDifficultyForRank(
                                        (Rank)(new Database.Player(s.SteamId).GetRank())) == (LevelDifficulty)s.DifficultyLevel) //If the score is invalid or the song doesn't exist, or the user played the wrong difficulty
                            {
                                Logger.Info($"RECEIVED VALID SCORE: {s.Score_} FOR {s.SteamId} {s.SongId} {s.DifficultyLevel}");
                            }
                            else
                            {
                                Logger.Warning($"RECEIVED INVALID SCORE {s.Score_} FROM {s.SteamId}");
                                return new HttpResponse()
                                {
                                    ReasonPhrase = "Bad Request",
                                    StatusCode = "400"
                                };
                            }

                            long oldScore = 0;
                            if (Database.Score.Exists(s.SongId, s.SteamId, s.DifficultyLevel))
                            {
                                oldScore = new Database.Score(s.SongId, s.SteamId, s.DifficultyLevel, s.GameplayMode).GetScore();
                            }

                            if (s.Score_ > oldScore)
                            {
                                Database.Player player = new Database.Player(s.SteamId);

                                //Player stats
                                if (oldScore > 0) player.IncrementPersonalBestsBeaten();
                                else player.IncrementSongsPlayed();
                                player.IncrementTotalScore(s.Score_ - oldScore); //Increment total score only by the amount the score has increased

                                new Database.Score(s.SongId, s.SteamId, s.DifficultyLevel, s.GameplayMode).SetScore(s.Score_, s.FullCombo);
                                
                                //Only send message if player is registered
                                if (Database.Player.Exists(s.SteamId)) Discord.CommunityBot.SendToScoreChannel($"User \"{player.GetDiscordMention()}\" has scored {s.Score_} on {new Database.Song(s.SongId, s.GameplayMode).GetSongName()}!");
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
                    Name = "Score Receiver",
                    UrlRegex = @"^/requestrank/$",
                    Method = "POST",
                    Callable = (HttpRequest request) => {
                        try
                        {
                            //Get JSON object from request content
                            JSONNode node = JSON.Parse(WebUtility.UrlDecode(request.Content));

                            //Get Score object from JSON
                            RankRequest r = RankRequest.Parser.ParseFrom(Convert.FromBase64String(node["pb"]));

                            if (RSA.SignRankRequest(Convert.ToUInt64(r.SteamId), (Rank)r.RequestedRank, r.IsInitialAssignment) == r.Signed &&
                                Database.Player.Exists(r.SteamId) &&
                                Database.Player.IsRegistered(r.SteamId))
                            {
                                Logger.Info($"RECEIVED VALID RANK REQUEST: {r.RequestedRank} FOR {r.SteamId} {r.IsInitialAssignment}");

                                Database.Player player = new Database.Player(r.SteamId);

                                //The rank up system will ignore requets where the player doesn't have the required tokens,
                                //or is requesting a rank higher than the one above their current rank (if it's not an inital rank assignment)
                                if (r.IsInitialAssignment && (Rank)player.GetRank() == Rank.None) Discord.CommunityBot.ChangeRank(player, (Rank)r.RequestedRank);
                                else if (player.GetRank() < (int)Rank.Gold) Discord.CommunityBot.ChangeRank(player, (Rank)(player.GetRank() + 1));
                                else if (player.GetRank() == (int)Rank.Gold && player.GetTokens() >= 3) { //Player is submitting for Blue
                                    player.SetTokens(player.GetTokens() - 3); //Take our toll ;)
                                    Discord.CommunityBot.SubmitBlueApplication(player, r.OstScoreList);
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
                                Logger.Warning($"RECEIVED INVALID RANK REQUEST {r.RequestedRank} FROM {r.SteamId}");
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
                    Name = "Weekly Song Getter",
                    UrlRegex = @"^/getweeklysongs/$",
                    Method = "GET",
                    Callable = (HttpRequest request) => {
                        JSONNode json = new JSONObject();
                        List<SongConstruct> songs = GetActiveSongs();

                        songs.ForEach(x => json.Add(x.SongId, x.Mode));

                        return new HttpResponse()
                        {
                            ContentAsUTF8 = json.ToString(),
                            ReasonPhrase = "OK",
                            StatusCode = "200"
                        };
                     }
                },
                //TODO: This is totally vulnerable to an attack where I could be spammed
                //with new id's and it'd fill the database with junk. Should verify with steam/oculus
                new Route {
                    Name = "Player Stats Getter",
                    UrlRegex = @"^/getplayerstats/",
                    Method = "GET",
                    Callable = (HttpRequest request) => {
                        string steamId = request.Path.Substring(request.Path.LastIndexOf("/") + 1);

                        if (!(steamId.Length == 17 || steamId.Length == 16 || steamId.Length == 15)) //17 = vive, 16 = oculus, 15 = sfkwww
                        {
                            Logger.Error($"Invalid id size: {steamId.Length}");
                            return new HttpResponse()
                            {
                                ReasonPhrase = "Bad Request",
                                StatusCode = "400"
                            };
                        }

                        JSONNode json = new JSONObject();

                        if (Database.Player.Exists(steamId)) {
                            Database.Player player = new Database.Player(steamId);

                            json["version"] = VersionCode;
                            json["rank"] = player.GetRank();
                            json["tokens"] = player.GetTokens();
                            json["projectedTokens"] = player.GetProjectedTokens();
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
                    UrlRegex = @"^/getsongleaderboards/",
                    Method = "GET",
                    Callable = (HttpRequest request) => {
                        string[] requestData = request.Path.Substring(1).Split('/');

                        string songId = requestData[1];
                        int rank = Convert.ToInt32(requestData[2]);
                        int mode = Convert.ToInt32(requestData[3]);
                        SongConstruct songConstruct = new SongConstruct()
                        {
                            SongId = songId,
                            Mode = requestData[3]
                        };

                        if (!Database.Song.Exists(songId, Convert.ToInt32(mode)))
                        {
                            Logger.Error($"Song doesn't exist for leaderboards: {songId} {mode}");
                            return new HttpResponse()
                            {
                                ReasonPhrase = "Bad Request",
                                StatusCode = "400"
                            };
                        }

                        IDictionary<string, ScoreConstruct> scores = GetScoresForSong(songConstruct, rank);
                        JSONNode json = new JSONObject();

                        int place = 1;
                        scores.Take(10).ToList().ForEach(x =>
                        {
                            JSONNode node = new JSONObject();
                            node["score"] = x.Value.Score;
                            node["player"] = new Database.Player(x.Key).GetDiscordName();
                            node["place"] = place;
                            node["fullCombo"] = x.Value.FullCombo ? "true" : "false";
                            node["steamId"] = x.Key;
                            node["rank"] = (int)x.Value.Rank;
                            json.Add(Convert.ToString(place++), node);
                        });

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

#if DEBUG
            int port = 3704; //My vhost is set up to direct to 3704 when the /api-beta/ route is followed
#else
            int port = 3703;
#endif
            HttpServer httpServer = new HttpServer(port, route_config);
            httpServer.Listen();
        }

        public static int Main(string[] args)
        {
            //Load server config
            Config.LoadConfig();

            //Start Discord bot
            Thread thread1 = new Thread(new ThreadStart(Discord.CommunityBot.Start));
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
