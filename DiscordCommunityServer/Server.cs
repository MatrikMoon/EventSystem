using TeamSaberShared;
using TeamSaberShared.SimpleJSON;
using SimpleHttpServer;
using SimpleHttpServer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using static TeamSaberServer.Database.SimpleSql;
using static TeamSaberShared.SharedConstructs;
using System.Text.RegularExpressions;
using TeamSaberServer.Database;

namespace TeamSaberServer
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

                            if (RSA.SignScore(Convert.ToUInt64(s.SteamId), s.SongId, s.DifficultyLevel, s.FullCombo, s.Score_) == s.Signed &&
                                Database.Song.Exists(s.SongId) &&
                                !new Database.Song(s.SongId).IsOld() &&
                                Database.Player.Exists(s.SteamId) &&
                                Database.Player.IsRegistered(s.SteamId))
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
                                oldScore = new Database.Score(s.SongId, s.SteamId, s.DifficultyLevel).GetScore();
                            }

                            if (s.Score_ > oldScore)
                            {
                                Database.Player player = new Database.Player(s.SteamId);

                                //Player stats
                                if (oldScore > 0) player.IncrementPersonalBestsBeaten();
                                else player.IncrementSongsPlayed();
                                player.IncrementTotalScore(s.Score_ - oldScore); //Increment total score only by the amount the score has increased

                                new Database.Score(s.SongId, s.SteamId, s.DifficultyLevel).SetScore(s.Score_, s.FullCombo);
                                
                                //Only send message if player is registered
                                if (Database.Player.Exists(s.SteamId)) Discord.CommunityBot.SendToScoreChannel($"User \"{player.GetDiscordMention()}\" has scored {s.Score_} on {new Database.Song(s.SongId).GetSongName()}!");
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
                    Name = "Weekly Song Getter",
                    UrlRegex = @"^/getweeklysongs/$",
                    Method = "GET",
                    Callable = (HttpRequest request) => {
                        JSONNode json = new JSONObject();
                        List<SongConstruct> songs = GetActiveSongs();

                        songs.ForEach(x => {
                            var item = new JSONObject();
                            item["songName"] = x.Name;
                            item["songId"] = x.SongId;
                            json.Add(x.SongId, item);
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
                    UrlRegex = @"^/getteams/$",
                    Method = "GET",
                    Callable = (HttpRequest request) => {
                        JSONNode json = new JSONObject();
                        List<Team> teams = GetAllTeams();

                        teams.ForEach(x => {
                            var item = new JSONObject();
                            item["captainId"] = x.GetCaptain();
                            item["teamName"] = x.GetTeamName();
                            item["color"] = x.GetColor();
                            json.Add(x.GetTeamId(), item);
                        });

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
                        steamId = Regex.Replace(steamId, "[^a-zA-Z0-9]", "");

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
                            json["rarity"] = player.GetRarity();
                            json["team"] = player.GetTeam();
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
                        int rarity = Convert.ToInt32(requestData[2]);
                        string teamId = requestData[3];
                        teamId = Regex.Replace(teamId, "[^a-zA-Z0-9-]", "");

                        Logger.Warning($"{songId} {(Rarity)rarity} {teamId}");

                        SongConstruct songConstruct = new SongConstruct()
                        {
                            SongId = songId,
                        };

                        if (!Database.Song.Exists(songId))
                        {
                            Logger.Error($"Song doesn't exist for leaderboards: {songId}");
                            return new HttpResponse()
                            {
                                ReasonPhrase = "Bad Request",
                                StatusCode = "400"
                            };
                        }

                        IDictionary<string, ScoreConstruct> scores = GetScoresForSong(songConstruct, rarity, teamId);
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
                            node["rarity"] = (int)x.Value.Rarity;
                            node["team"] = x.Value.TeamId;
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
            int port = 3708; //My vhost is set up to direct to 3708 when the /api-beta/ route is followed
#else
            int port = 3707;
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
