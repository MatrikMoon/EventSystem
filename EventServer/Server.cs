﻿using EventServer.Database;
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
using static EventServer.Database.SimpleSql;
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

                            if (RSA.SignScore(Convert.ToUInt64(s.UserId), s.SongId, s.Difficulty, s.FullCombo, s.Score_, s.PlayerOptions, s.GameOptions) == s.Signed &&
                                Song.Exists(s.SongId, (LevelDifficulty)s.Difficulty, true) &&
                                !new Song(s.SongId, (LevelDifficulty)s.Difficulty).Old &&
                                Player.Exists(s.UserId) &&
                                Player.IsRegistered(s.UserId))
                            {
                                Logger.Info($"RECEIVED VALID SCORE: {s.Score_} FOR {new Player(s.UserId).DiscordName} {s.SongId} {s.Difficulty}");
                            }
                            else
                            {
                                Logger.Error($"RECEIVED INVALID SCORE {s.Score_} FROM {new Player(s.UserId).DiscordName} FOR {s.UserId} {s.SongId} {s.Difficulty}");
                                return new HttpResponse()
                                {
                                    ReasonPhrase = "Bad Request",
                                    StatusCode = "400"
                                };
                            }

                            Database.Score oldScore = null;
                            if (Database.Score.Exists(s.SongId, s.UserId, (LevelDifficulty)s.Difficulty))
                            {
                                oldScore = new Database.Score(s.SongId, s.UserId, (LevelDifficulty)s.Difficulty);
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

                                Database.Score newScore = new Database.Score(s.SongId, s.UserId, (LevelDifficulty)s.Difficulty);
                                newScore.SetScore(s.Score_, s.FullCombo);

                                //Only send message if player is registered
                                if (Player.Exists(s.UserId)) Discord.CommunityBot.SendToScoreChannel($"User \"{player.DiscordMention}\" has scored {s.Score_} on {new Song(s.SongId, (LevelDifficulty)s.Difficulty).SongName} ({(LevelDifficulty)s.Difficulty})!");
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
                    Name = "Song Getter",
                    UrlRegex = @"^/songs/",
                    Method = "GET",
                    Callable = (HttpRequest request) => {
                        string[] requestData = request.Path.Substring(1).Split('/');
                        string userId = requestData[1];
                        userId = Regex.Replace(userId, "[^a-zA-Z0-9- ]", "");

                        if (!Player.Exists(userId) || !Player.IsRegistered(userId))
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

                            if (x.Difficulty == LevelDifficulty.Auto) x.Difficulty = new Player(userId).GetPreferredDifficulty(OstHelper.IsOst(x.SongId));

                            var item = new JSONObject();
                            item["songName"] = x.Name;
                            item["songId"] = x.SongId;
                            item["difficulty"] = (int)x.Difficulty;
                            item["gameOptions"] = (int)x.GameOptions;
                            item["playerOptions"] = (int)x.PlayerOptions;
                            json.Add(x.SongId + (int)x.Difficulty, item);
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

                        teams.ForEach(x => {
                            var item = new JSONObject();
                            item["captainId"] = x.Captain;
                            item["teamName"] = x.TeamName;
                            item["color"] = x.Color;
                            item["score"] = x.Score;
                            json.Add(x.TeamId, item);
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
                    UrlRegex = @"^/playerstats/",
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

                        if (Player.Exists(steamId) && Player.IsRegistered(steamId)) {
                            Player player = new Player(steamId);

                            json["version"] = VersionCode;
                            json["rarity"] = player.Rarity;
                            json["team"] = player.Team;
                        }
                        else if (Player.Exists(steamId) && !Player.IsRegistered(steamId))
                        {
                            json["message"] = "Your team has been eliminated.";
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
                        string songId = requestData[1];
                        JSONNode json = new JSONObject();

                        if (songId == "all")
                        {
                            int take = 10;
                            int rarity = (int)Rarity.All;
                            string teamId = "-1";

                            if (requestData.Length > 3) take = Convert.ToInt32(requestData[2]);
                            if (requestData.Length > 4) rarity = Convert.ToInt32(requestData[3]);
                            if (requestData.Length > 5) teamId = requestData[4];

                            List<SongConstruct> songs = GetAllScores((Rarity)rarity, teamId);
                            songs.ToList().ForEach(x =>
                            {
                                JSONNode songNode = new JSONObject();
                                songNode["songId"] = x.SongId;
                                songNode["songName"] = x.Name;
                                songNode["difficulty"] = (int)x.Difficulty;
                                songNode["scores"] = new JSONObject();

                                int place = 1;
                                x.Scores.Take(take).ToList().ForEach(y =>
                                {
                                    JSONNode scoreNode = new JSONObject();
                                    scoreNode["score"] = y.Score;
                                    scoreNode["player"] = new Player(y.PlayerId).DiscordName;
                                    scoreNode["place"] = place;
                                    scoreNode["fullCombo"] = y.FullCombo ? "true" : "false";
                                    scoreNode["steamId"] = y.PlayerId;
                                    scoreNode["rarity"] = (int)y.Rarity;
                                    scoreNode["team"] = y.TeamId;
                                    songNode["scores"].Add(Convert.ToString(place++), scoreNode);
                                });

                                json.Add(x.SongId + ":" + (int)x.Difficulty, songNode);
                            });
                        }
                        else
                        {
                            int difficulty = Convert.ToInt32(requestData[2]);
                            int rarity = Convert.ToInt32(requestData[3]);
                            string teamId = requestData[4];
                            songId = Regex.Replace(songId, "[^a-zA-Z0-9- ]", "");
                            teamId = Regex.Replace(teamId, "[^a-zA-Z0-9- ]", "");

                            SongConstruct songConstruct = new SongConstruct()
                            {
                                SongId = songId,
                                Difficulty = (LevelDifficulty)difficulty
                            };

                            if (!Song.Exists(songId, (LevelDifficulty)difficulty, true))
                            {
                                Logger.Error($"Song doesn't exist for leaderboards: {songId}");
                                return new HttpResponse()
                                {
                                    ReasonPhrase = "Bad Request",
                                    StatusCode = "400"
                                };
                            }

                            List<ScoreConstruct> scores = GetScoresForSong(songConstruct, (Rarity)rarity, teamId);

                            int place = 1;
                            scores.Take(10).ToList().ForEach(x =>
                            {
                                JSONNode node = new JSONObject();
                                node["score"] = x.Score;
                                node["player"] = new Player(x.PlayerId).DiscordName;
                                node["place"] = place;
                                node["fullCombo"] = x.FullCombo ? "true" : "false";
                                node["steamId"] = x.PlayerId;
                                node["rarity"] = (int)x.Rarity;
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

#if (DEBUG && TEAMSABER)
            int port = 3708; //My vhost is set up to direct to 3708 when the /api-teamsaber-beta/ route is followed
#elif (!DEBUG && TEAMSABER)
            int port = 3707;
#elif (DEBUG && DISCORDCOMMUNITY)
            int port = 3704; //My vhost is set up to direct to 3708 when the /api-beta/ route is followed
#elif (!DEBUG && DISCORDCOMMUNITY)
            int port = 3703;
#elif DEBUG
            int port = 3704; //My vhost is set up to direct to 3708 when the /api-beta/ route is followed
#elif !DEBUG
            int port = 3703;
#endif
            HttpServer httpServer = new HttpServer(port, route_config);
            httpServer.Listen();
        }

        public static int Main(string[] args)
        {
            var score = new Score("USERID", "SONGID", 123456, 4, false, 2, 4, "SIGNED");
            var base64 = score.ToBase64();
            Logger.Warning(base64);
            var newScore = Score.FromString(base64);
            Logger.Success($"{newScore.Score_} {newScore.UserId}");

            //var attemptScore = "bW9vbjM3My0yMjMANzY1NjExOTgwNjMyNjgyNTEAAAAAAAAAAAAAAAAAAAAAAABIZ1E5NzIxSFNhUWlJbG1RREwvekQ5YXJHZU4raHlYRUlRRzdFTVpxa3FtejdwVHZ6RTM1Sk90aTl3ZythWTZ4WXVTbUQ3V2RNNDVRQUp6bWQ4azFQZ1JnSzRRN0tNWFkwdXhqK3NDWk1rR0NIYlZtQ2lKcUQ2OElBVTUxU3dNV3hjUi9Hb3ByQTFabmphUE10YmF6ZC9rS3dYQ2FSalNjbGU5OXJmenR4cHI5SUMxTXRiTHV6WFl4MmszYUhROFAzYWlsTjlPeGJvM00wL00xK3d4Z2ZuRG53REpsVGR1UUFqM1RTMzN5aWl0aCs2VnY1U0tHeEozb3drQzlFUGtYdmVLUGNZZ0VWOTlkaVFHbjJoRFMrRDVxLzZsT25FQmlOaE1MYVRJTW5raDNBS1V0NnB6RXFoQ0ZmZnViTVFRTTY4M2FWNkgrNWV4RnNzc3RnckhaVGc9PQA=";
            var attemptScore = "bW9vbjc2NTYxMTk4MDYzMjY4MjUxADM3My0yMjMAAAAAAAAAAAAAAAAAAAAAAABIZ1E5NzIxSFNhUWlJbG1RREwvekQ5YXJHZU4raHlYRUlRRzdFTVpxa3FtejdwVHZ6RTM1Sk90aTl3ZythWTZ4WXVTbUQ3V2RNNDVRQUp6bWQ4azFQZ1JnSzRRN0tNWFkwdXhqK3NDWk1rR0NIYlZtQ2lKcUQ2OElBVTUxU3dNV3hjUi9Hb3ByQTFabmphUE10YmF6ZC9rS3dYQ2FSalNjbGU5OXJmenR4cHI5SUMxTXRiTHV6WFl4MmszYUhROFAzYWlsTjlPeGJvM00wL00xK3d4Z2ZuRG53REpsVGR1UUFqM1RTMzN5aWl0aCs2VnY1U0tHeEozb3drQzlFUGtYdmVLUGNZZ0VWOTlkaVFHbjJoRFMrRDVxLzZsT25FQmlOaE1MYVRJTW5raDNBS1V0NnB6RXFoQ0ZmZnViTVFRTTY4M2FWNkgrNWV4RnNzc3RnckhaVGc9PQA=";
            var attempt = Score.FromString(attemptScore);

            Logger.Success($"{attempt.UserId} {attempt.SongId} {attempt.Score_} {attempt.FullCombo} {attempt.Signed}");

            //Load server config
            Config.LoadConfig();

            //Start Discord bot
            Thread thread1 = new Thread(new ThreadStart(CommunityBot.Start));
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
