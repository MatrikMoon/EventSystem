using System;
using System.Net.Http;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using DiscordCommunityServer.Discord.Services;
using System.Collections.Generic;
using static DiscordCommunityServer.Database.SimpleSql;
using static DiscordCommunityShared.SharedConstructs;
using DiscordCommunityServer.Database;

namespace DiscordCommunityServer.Discord
{
    class CommunityBot
    {
        private static DiscordSocketClient _client;
        private static IServiceProvider _services;

        //Following should be private, except for the fact we're keeping legacy
        //support for people who already have a rank assigned to them.
        //This means when someone registers, that command needs access to these
        //to see if they're assigned a role that's relevant to us.
        //Also leaderboards uses _ranksToList
        public static int[] _saberRankValues = { 0, 1, 2, 3, 4, 5 };
        public static string[] _saberRoles = { "white", "bronze", "silver", "gold", "blue", "master" };
        public static Rank[] _ranksToList = { Rank.Master, Rank.Blue, Rank.Gold,
                                            Rank.Silver, Rank.Bronze };

        public static void Start()
        {
            MainAsync().GetAwaiter().GetResult();
        }

        public static void SendToScoreChannel(string message)
        {
            if (_client.ConnectionState == ConnectionState.Connected)
            {
#if DEBUG
                var guild = _client.Guilds.ToList().Where(x => x.Name.Contains("Beat Saber Testing Server")).First();
#else
                var guild = _client.Guilds.ToList().Where(x => x.Name.Contains("Beat Saber Discord Server")).First();
#endif
                guild.TextChannels.ToList().Where(x => x.Name == "event-scores").First().SendMessageAsync(message);
            }
        }

        public static async void ChangeRank(Player player, Rank rank)
        {
#if DEBUG
            var guild = _client.Guilds.ToList().Where(x => x.Name.Contains("Beat Saber Testing Server")).First();
#else
            var guild = _client.Guilds.ToList().Where(x => x.Name.Contains("Beat Saber Discord Server")).First();
#endif
            var user = guild.Users.Where(x => x.Mention == player.GetDiscordMention()).First();
            var rankChannel = guild.TextChannels.ToList().Where(x => x.Name == "event-scores").First();

            player.SetRank((int)rank);
            await user.RemoveRolesAsync(guild.Roles.Where(x => _saberRoles.Contains(x.Name.ToLower())));
            await user.AddRoleAsync(guild.Roles.FirstOrDefault(x => x.Name.ToLower() == rank.ToString().ToLower()));

            //Sort out existing scores
            string steamId = player.GetSteamId();
            IDictionary<SongConstruct, ScoreConstruct> playerScores = GetScoresForPlayer(steamId);
            string replaySongs = string.Empty;

            if (playerScores.Count >= 0)
            {
                playerScores.ToList().ForEach(x =>
                {
                    BeatSaver.Song songData = new BeatSaver.Song(x.Key.SongId);
                    if (songData.GetDifficultyForRank(rank) != x.Value.Difficulty)
                    {
                        replaySongs += songData.SongName + "\n";
                        ExecuteCommand($"UPDATE scoreTable SET old = 1 WHERE songId=\'{x.Key.SongId}\' AND steamId=\'{steamId}\'");
                    }
                    else
                    {
                        ExecuteCommand($"UPDATE scoreTable SET rank = {(int)rank} WHERE songId=\'{x.Key.SongId}\' AND steamId=\'{steamId}\'");
                    }
                });
            }

            if (replaySongs != string.Empty)
            {
                string reply = $"{player.GetDiscordMention()} has been promoted to {rank}!\n" +
                    "You'll need to re-play the following songs:\n" +
                    replaySongs;
                await rankChannel.SendMessageAsync(reply);
            }

            else
            {
                await rankChannel.SendMessageAsync($"{player.GetDiscordMention()} has been promoted to {rank}!");
            }
        }

        public static async void SubmitBlueApplication(Player player, string ostScoreString)
        {
#if DEBUG
            var guild = _client.Guilds.ToList().Where(x => x.Name.Contains("Beat Saber Testing Server")).First();
#else
            var guild = _client.Guilds.ToList().Where(x => x.Name.Contains("Beat Saber Discord Server")).First();
#endif
            var user = guild.Users.Where(x => x.Mention == player.GetDiscordMention()).First();
            var voteChannel = guild.TextChannels.ToList().Where(x => x.Name == "vote").First();

            var message = await voteChannel.SendMessageAsync(
                $"{player.GetDiscordName()} has submitted an application.\n" +
                $"https://scoresaber.com/u/{player.GetSteamId()}\n\n" +
                $"{ostScoreString}");

            var commandHandlingService = _services.GetRequiredService<CommandHandlingService>();

            int votes = 0;
            Action<IUserMessage, SocketReaction> reactionRemoved = (m, r) =>
            {
                if (m.Id == message.Id && r.Emote.Name == "accepted") votes--;
            };
            commandHandlingService.ReactionRemoved += reactionRemoved;

            Action<IUserMessage, SocketReaction> reactionAdded = null;
            reactionAdded = (m, r) =>
            {
                if (m.Id == message.Id && r.Emote.Name == "accepted")
                {
#if DEBUG
                    if (++votes >= 1)
#else
                    if (++votes >= 8)
#endif
                    {
                        ChangeRank(player, Rank.Blue);
                        commandHandlingService.ReactionAdded -= reactionAdded;
                        commandHandlingService.ReactionRemoved -= reactionRemoved;
                    }
                }
            };
            commandHandlingService.ReactionAdded += reactionAdded;
        }

        public static async Task MainAsync()
        {
            _services = ConfigureServices();

            _client = _services.GetRequiredService<DiscordSocketClient>();

            _client.Log += LogAsync;
            _services.GetRequiredService<CommandService>().Log += LogAsync;

#if DEBUG
            await _client.LoginAsync(TokenType.Bot, Config.BetaBotToken);
#else
            await _client.LoginAsync(TokenType.Bot, Config.BotToken);
#endif
            await _client.StartAsync();
            await _services.GetRequiredService<CommandHandlingService>().InitializeAsync();

            await Task.Delay(-1);
        }

        private static Task LogAsync(LogMessage log)
        {
            Console.WriteLine(log.ToString());

            return Task.CompletedTask;
        }

        private static IServiceProvider ConfigureServices()
        {
            var config = new DiscordSocketConfig { MessageCacheSize = 100 };
            var client = new DiscordSocketClient(config);
            return new ServiceCollection()
                .AddSingleton(client)
                .AddSingleton<CommandService>()
                .AddSingleton<CommandHandlingService>()
                .AddSingleton<HttpClient>()
                .AddSingleton<PictureService>()
                .BuildServiceProvider();
        }
    }
}
