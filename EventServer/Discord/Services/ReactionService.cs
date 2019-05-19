using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace EventServer.Discord.Services
{
    public class ReactionService
    {
        public DiscordSocketClient _discordClient;

        public event Action<SocketReaction> ReactionAdded;
        public event Action<SocketReaction> ReactionRemoved;

        public ReactionService(IServiceProvider services)
        {
            _discordClient = services.GetRequiredService<DiscordSocketClient>();

            _discordClient.ReactionAdded += ReactionAddedAsync;
            _discordClient.ReactionRemoved += ReactionRemovedAsync;
        }

        private Task ReactionAddedAsync(Cacheable<IUserMessage, ulong> before, ISocketMessageChannel channel, SocketReaction reaction)
        {
            ReactionAdded?.Invoke(reaction);
            return Task.CompletedTask;
        }

        private Task ReactionRemovedAsync(Cacheable<IUserMessage, ulong> before, ISocketMessageChannel channel, SocketReaction reaction)
        {
            ReactionRemoved?.Invoke(reaction);
            return Task.CompletedTask;
        }
    }
}
