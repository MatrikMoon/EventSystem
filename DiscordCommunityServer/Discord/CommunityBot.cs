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
        //support for people who already have a rarity assigned to them.
        //This means when someone registers, that command needs access to these
        //to see if they're assigned a role that's relevant to us.
        //Also leaderboards uses _rarityToList
        public static int[] _saberRankValues = { 0, 1, 2, 3, 4, 5 };
        public static string[] _saberRoles = { "uncommon", "rare", "epic", "legendary", "mythic", "captain" };
        public static Rarity[] _rarityToList = { Rarity.Captain, Rarity.Mythic, Rarity.Legendary,
                                            Rarity.Epic, Rarity.Rare, Rarity.Uncommon };

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

        public static async void ChangeRarity(Player player, Rarity rarity)
        {
#if DEBUG
            var guild = _client.Guilds.ToList().Where(x => x.Name.Contains("Beat Saber Testing Server")).First();
#else
            var guild = _client.Guilds.ToList().Where(x => x.Name.Contains("Beat Saber Discord Server")).First();
#endif
            var user = guild.Users.Where(x => x.Mention == player.GetDiscordMention()).First();
            var rankChannel = guild.TextChannels.ToList().Where(x => x.Name == "event-scores").First();

            player.SetRarity((int)rarity);
            await user.RemoveRolesAsync(guild.Roles.Where(x => _saberRoles.Contains(x.Name.ToLower())));
            await user.AddRoleAsync(guild.Roles.FirstOrDefault(x => x.Name.ToLower() == rarity.ToString().ToLower()));

            //Sort out existing scores
            string steamId = player.GetSteamId();
            IDictionary<SongConstruct, ScoreConstruct> playerScores = GetScoresForPlayer(steamId);

            if (playerScores.Count >= 0)
            {
                playerScores.ToList().ForEach(x =>
                {
                    BeatSaver.Song songData = new BeatSaver.Song(x.Key.SongId);
                    ExecuteCommand($"UPDATE scoreTable SET rarity = {(int)rarity} WHERE songId=\'{x.Key.SongId}\' AND steamId=\'{steamId}\'");
                });
            }

            else
            {
                await rankChannel.SendMessageAsync($"{player.GetDiscordMention()} has been promoted to {rarity}!");
            }
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
