using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace TeamSaberServer.Discord.Services
{
    public class CommandHandlingService

    {
        //TODO: This is probably not how I'm supposed to be doing this
        public event Action<IUserMessage, SocketReaction> ReactionAdded;
        public event Action<IUserMessage, SocketReaction> ReactionRemoved;

        private readonly CommandService _commands;
        private readonly DiscordSocketClient _discord;
        private readonly IServiceProvider _services;

        public CommandHandlingService(IServiceProvider services)
        {
            _commands = services.GetRequiredService<CommandService>();
            _discord = services.GetRequiredService<DiscordSocketClient>();
            _services = services;

            _discord.MessageReceived += MessageReceivedAsync;
            _discord.MessageUpdated += MessageUpdatedAsync;
            _discord.ReactionAdded += ReactionAddedAsync;
            _discord.ReactionRemoved += ReactionRemovedAsync;
            _discord.UserVoiceStateUpdated += UserVoiceStateUpdated;
        }

        public async Task InitializeAsync()
        {
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly());
        }

        public async Task MessageReceivedAsync(SocketMessage rawMessage)
        {
            // Ignore system messages, or messages from other bots
            if (!(rawMessage is SocketUserMessage message)) return;
            if (message.Source != MessageSource.User) return;

            // This value holds the offset where the prefix ends
            var argPos = 0;
            if (!message.HasMentionPrefix(_discord.CurrentUser, ref argPos)) return;

            var context = new SocketCommandContext(_discord, message);
            var result = await _commands.ExecuteAsync(context, argPos, _services);

            if (result.Error.HasValue &&
                result.Error.Value != CommandError.UnknownCommand) // it's bad practice to send 'unknown command' errors
                await context.Channel.SendMessageAsync(result.ToString());
        }

        private async Task MessageUpdatedAsync(Cacheable<IMessage, ulong> before, SocketMessage after, ISocketMessageChannel channel)
        {
            var message = await before.GetOrDownloadAsync();
            Console.WriteLine($"{message} -> {after}");
        }

        private async Task ReactionAddedAsync(Cacheable<IUserMessage, ulong> before, ISocketMessageChannel channel, SocketReaction reaction)
        {
            var message = await before.GetOrDownloadAsync();
            ReactionAdded?.Invoke(message, reaction);
        }

        private async Task ReactionRemovedAsync(Cacheable<IUserMessage, ulong> before, ISocketMessageChannel channel, SocketReaction reaction)
        {
            var message = await before.GetOrDownloadAsync();
            ReactionRemoved?.Invoke(message, reaction);
        }

        private async Task UserVoiceStateUpdated(SocketUser socketUser, SocketVoiceState socketVoiceState, SocketVoiceState userVoiceStateUpdated)
        {

        }
    }
}
