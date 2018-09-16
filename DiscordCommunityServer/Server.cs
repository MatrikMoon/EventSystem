using DiscordCommunityShared;
using DiscordCommunityShared.SimpleJSON;
using SimpleHttpServer;
using SimpleHttpServer.Models;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;

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

                            if (RSA.SignScore(Convert.ToUInt64(s.SteamId), s.SongId, s.DifficultyLevel, s.GameplayMode, s.Score_) == s.Signed)
                            {
                                Logger.Info($"RECEIVED VALID SCORE: {s.Score_}");
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

                                new Database.Score(s.SongId, s.SteamId, s.DifficultyLevel, s.GameplayMode, s.Score_);
                                Discord.CommunityBot.SendToScoreChannel($"User \"{player.GetDiscordMention()}\" has scored {s.Score_} on {new Database.Song(s.SongId, s.GameplayMode).GetSongName()}!");
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
                        List<Dictionary<string, string>> songs = Database.SimpleSql.ExecuteQuery("SELECT songId, mode FROM songTable WHERE old = 0", "songId", "mode");

                        songs.ForEach(x => json.Add(x["songId"], x["mode"]));

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
                        Logger.Success($"Request path: {request.Path}");

                        string steamId = request.Path.Substring(request.Path.LastIndexOf("/") + 1);

                        if (steamId.Length != 17)
                        {
                            Logger.Error($"Invalid id size: {steamId.Length}");
                            return new HttpResponse()
                            {
                                ReasonPhrase = "Bad Request",
                                StatusCode = "400"
                            };
                        }

                        Database.Player player = new Database.Player(steamId);

                        JSONNode json = new JSONObject();
                        json["version"] = SharedConstructs.VersionCode;
                        json["rank"] = player.GetRank();
                        json["tokens"] = player.GetTokens();

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
