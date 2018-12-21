using DiscordCommunityServer.Database;
using ChristmasShared;
using ChristmasShared.SimpleJSON;
using SimpleHttpServer;
using SimpleHttpServer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using static DiscordCommunityServer.Database.SimpleSql;
using static ChristmasShared.SharedConstructs;

namespace DiscordCommunityServer
{
    class Server
    {
        private static readonly Regex IdRegex = new Regex(@"([0-9])*(-)*", RegexOptions.Compiled);

        public static void StartHttpServer()
        {
            var route_config = new List<Route>() {
                new Route {
                    Name = "Vote Receiver",
                    UrlRegex = @"/submitvote/$",
                    Method = "POST",
                    Callable = (HttpRequest request) => {
                        try
                        {
                            //Get JSON object from request content
                            JSONNode node = JSON.Parse(WebUtility.UrlDecode(request.Content));

                            //Get Score object from JSON
                            Vote v = Vote.Parser.ParseFrom(Convert.FromBase64String(node["pb"]));
                            if (E.doE(Convert.ToUInt64(v.UserId), v.ItemId, (Category)v.Category) == v.Signed &&
                                IdRegex.IsMatch(v.ItemId) &&
                                Item.Exists(v.ItemId) &&
                                !Database.Vote.Exists(v.UserId, v.ItemId)
                                //Player.Exists(v.UserId) &&
                                //Player.IsRegistered(v.SteamId)
                                )
                            {
                                Logger.Info($"RECEIVED VALID VOTE: {v.UserId} FOR {v.Category} {v.ItemId}");

                                Database.Vote.RemoveExsitingVoteForCategory(v.UserId, (Category)v.Category);
                                new Database.Vote(v.UserId, v.ItemId, (Category)v.Category);

                                return new HttpResponse()
                                {
                                    ReasonPhrase = "OK",
                                    StatusCode = "200"
                                };
                            }
                            else
                            {
                                Logger.Warning($"RECEIVED INVALID VOTE {v.ItemId} FROM {v.UserId}");
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
                    Name = "Item Getter",
                    UrlRegex = @"/getitems/",
                    Method = "GET",
                    Callable = (HttpRequest request) => {
                        JSONNode json = new JSONObject();
                        List<Item> items = GetActiveItems(Category.None);

                        items.ForEach(x => {
                            var item = new JSONObject();
                            item["name"] = x.GetItemName();
                            item["author"] = x.GetItemAuthor();
                            item["subName"] = x.GetItemSubname();
                            item["category"] = (int)x.Category;
                            item["itemId"] = x.ItemId;
                            json.Add(x.ItemId, item);
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
                    Name = "User Data Getter",
                    UrlRegex = @"/getuserdata/",
                    Method = "GET",
                    Callable = (HttpRequest request) => {
                        string userId = request.Path.Substring(request.Path.LastIndexOf("/") + 1);
                        userId = Regex.Replace(userId, "[^0-9]", "");

                        JSONNode json = new JSONObject();
                        List<Item> items = GetVotesForPlayer(userId);

                        items.ForEach(x => {
                            var item = new JSONObject();
                            item["name"] = x.GetItemName();
                            item["author"] = x.GetItemAuthor();
                            item["subName"] = x.GetItemSubname();
                            item["category"] = (int)x.Category;
                            item["itemId"] = x.ItemId;
                            json.Add(x.ItemId, item);
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
            int port = 3706; //My vhost is set up to direct to 3704 when the /api-beta/ route is followed
#else
            int port = 3705;
#endif
            HttpServer httpServer = new HttpServer(80, route_config);
            httpServer.Listen();
        }

        public static int Main(string[] args)
        {
            //Load server config
            Config.LoadConfig();

            //Start Discord bot
            //Thread thread1 = new Thread(new ThreadStart(Discord.CommunityBot.Start));
            //thread1.Start();

            //Set up HTTP server
            Thread thread2 = new Thread(new ThreadStart(StartHttpServer));
            thread2.Start();

            //Kill on enter press
            Console.ReadLine();
            return 0;
        }
    }
}
