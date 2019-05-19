using Discord;
using Discord.Commands;
using Discord.Rest;
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
using static EventServer.Database.SqlUtils;

namespace EventServer.Discord
{
    class CommunityBot
    {
        private static DiscordSocketClient _client;
        private static IServiceProvider _services;
        private static string _serverName;
        private static string _scoreChannel;
        private static string _voteChannel;
        private static string _databaseLocation;

        public static List<string> AdminRoles { get; } = new List<string>
        {
            "moderator",
            "weekly event manager",
            "senpai"
        };

        public static void Start(string serverName, string scoreChannel, string voteChannel, string databaseLocation = "botDatabase.db")
        {
            _serverName = serverName;
            _scoreChannel = scoreChannel;
            _voteChannel = voteChannel;
            _databaseLocation = databaseLocation;
            MainAsync().GetAwaiter().GetResult();
        }

        public static void SendToScoreChannel(string message)
        {
            var guild = _client.Guilds.ToList().Where(x => x.Name.Contains(_serverName)).First();
            guild.TextChannels.First(x => x.Name == _scoreChannel).SendMessageAsync(message);
        }

        public static Task<RestUserMessage> SendToVoteChannel(string message)
        {
            var guild = _client.Guilds.ToList().Where(x => x.Name.Contains(_serverName)).First();
            return guild.TextChannels.First(x => x.Name == _voteChannel).SendMessageAsync(message);
        }

        public static async void ChangeTeam(Player player, Team team, bool captain = false)
        {
            var guild = _client.Guilds.ToList().Where(x => x.Name.Contains(_serverName)).First();
            var user = guild.Users.Where(x => x.Mention == player.DiscordMention).First();
            var rankChannel = guild.TextChannels.ToList().Where(x => x.Name == _scoreChannel).First();

            //Add the role of the team we're being switched to
            //Note that this WILL NOT remove the role of the team the player is currently on, if there is one.
            await user.RemoveRoleAsync(guild.Roles.FirstOrDefault(x => Regex.Replace(x.Name.ToLower(), "[^a-z0-9 ]", "") == new Team(player.Team).TeamName.ToLower()));
            await user.AddRoleAsync(guild.Roles.FirstOrDefault(x => Regex.Replace(x.Name.ToLower(), "[^a-z0-9 ]", "") == team.TeamName.ToLower()));

            player.Team = team.TeamId;

            //Sort out existing scores
            string steamId = player.PlayerId;
            IDictionary<SongConstruct, ScoreConstruct> playerScores = GetScoresForPlayer(steamId);

            if (playerScores.Count >= 0)
            {
                playerScores.ToList().ForEach(x =>
                {
                    BeatSaver.Song songData = new BeatSaver.Song(x.Key.SongId);
                    ExecuteCommand($"UPDATE scoreTable SET team = \'{team.TeamId}\' WHERE songId=\'{x.Key.SongId}\' AND steamId=\'{steamId}\'");
                });
            }

            if (captain) team.Captain = player.PlayerId;

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

            _services.GetRequiredService<DatabaseService>().RegisterReactionRolesWithBot();
        }

        public static IServiceProvider GetServices() => _services;

        private static Task LogAsync(LogMessage log)
        {
            Console.WriteLine(log.ToString());

            return Task.CompletedTask;
        }

        private static IServiceProvider ConfigureServices()
        {
            var config = new DiscordSocketConfig { MessageCacheSize = 100 };
            return new ServiceCollection()
                .AddSingleton(new DiscordSocketClient(config))
                .AddSingleton<CommandService>()
                .AddSingleton<CommandHandlingService>()
                .AddSingleton<HttpClient>()
                .AddSingleton<PictureService>()
                .AddSingleton<ReactionService>()
                .AddSingleton(serviceProvider => new DatabaseService(_databaseLocation, serviceProvider))
                .BuildServiceProvider();
        }
    }
}
