using System;
using System.Net.Http;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using DiscordCommunityServer.Discord.Services;

namespace DiscordCommunityServer.Discord
{
    class CommunityBot
    {
        private static DiscordSocketClient _client;

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

        public static async Task MainAsync()
        {
            var services = ConfigureServices();

            _client = services.GetRequiredService<DiscordSocketClient>();

            _client.Log += LogAsync;
            services.GetRequiredService<CommandService>().Log += LogAsync;

#if DEBUG
            await _client.LoginAsync(TokenType.Bot, Config.BetaBotToken);
#else
            await _client.LoginAsync(TokenType.Bot, Config.BotToken);
#endif
            await _client.StartAsync();
            await services.GetRequiredService<CommandHandlingService>().InitializeAsync();

            await Task.Delay(-1);
        }

        private static Task LogAsync(LogMessage log)
        {
            Console.WriteLine(log.ToString());

            return Task.CompletedTask;
        }

        private static IServiceProvider ConfigureServices()
        {
            return new ServiceCollection()
                .AddSingleton<DiscordSocketClient>()
                .AddSingleton<CommandService>()
                .AddSingleton<CommandHandlingService>()
                .AddSingleton<HttpClient>()
                .AddSingleton<PictureService>()
                .BuildServiceProvider();
        }
    }
}
