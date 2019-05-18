using Discord;
using Discord.Commands;
using Discord.WebSocket;
using EventServer.Database;
using EventServer.Discord.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static EventServer.Database.SimpleSql;
using static EventShared.SharedConstructs;
using EventShared;

namespace EventServer.Discord
{
    class CommunityBot
    {
        private static DiscordSocketClient _client;
        private static IServiceProvider _services;
        private static string _serverName;
        private static string _scoreChannel;

        public static void Start(string serverName, string scoreChannel)
        {
            _serverName = serverName;
            _scoreChannel = scoreChannel;
            MainAsync().GetAwaiter().GetResult();
        }

        public static void SendToScoreChannel(string message)
        {
            SendToChannel(_scoreChannel, message);
        }

        public static void SendToChannel(string channel, string message)
        {
            if (_client.ConnectionState == ConnectionState.Connected)
            {
                var guild = _client.Guilds.ToList().Where(x => x.Name.Contains(_serverName)).First();
                guild.TextChannels.ToList().Where(x => x.Name == channel).First().SendMessageAsync(message);
            }
        }

        public static async void ChangeTeam(Player player, Team team, bool captain = false)
        {
            var guild = _client.Guilds.ToList().Where(x => x.Name.Contains(_serverName)).First();
            var user = guild.Users.Where(x => x.Mention == player.DiscordMention).First();
            var rankChannel = guild.TextChannels.ToList().Where(x => x.Name == _scoreChannel).First();

            //Add the role of the team we're being switched to
            //Note that this WILL NOT remove the role of the team the player is currently on, if there is one.
            await user.AddRoleAsync(guild.Roles.FirstOrDefault(x => Regex.Replace(x.Name.ToLower(), "[^a-z0-9 ]", "") == team.TeamName.ToLower()));

            player.Team = team.TeamId;

            //Sort out existing scores
            string steamId = player.SteamId;
            IDictionary<SongConstruct, ScoreConstruct> playerScores = GetScoresForPlayer(steamId);

            if (playerScores.Count >= 0)
            {
                playerScores.ToList().ForEach(x =>
                {
                    BeatSaver.Song songData = new BeatSaver.Song(x.Key.SongId);
                    ExecuteCommand($"UPDATE scoreTable SET team = \'{team.TeamId}\' WHERE songId=\'{x.Key.SongId}\' AND steamId=\'{steamId}\'");
                });
            }

            if (captain) team.Captain = player.SteamId;

            string teamName = team.TeamName;
            await rankChannel.SendMessageAsync($"{player.DiscordMention} has been assigned {(captain ? "as the captain of" : "to")} `{((teamName == string.Empty || teamName == null )? team.TeamId: teamName)}`!");
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
