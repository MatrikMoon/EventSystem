using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

/**
 * Created by Moon on 5/18/2019
 * Service for receiving events regarding message updates
 * This class could easily be removed in favor of using the
 * DiscordClientSocket's events directly, however, I've made
 * slight modifications to the events fired here, and also
 * I'd rather not inject DiscordClientSocket into all classes
 * where I need to deal with message updates. Some of you may
 * disagree with this desire, and I almost do myself. However,
 * it's aready here, so it'll stay until it becomes a problem.
 */

namespace EventServer.Discord.Services
{
    public class MessageUpdateService
    {
        public DiscordSocketClient _discordClient;

        public event Action<SocketReaction> ReactionAdded;
        public event Action<SocketReaction> ReactionRemoved;
        public event Action<Cacheable<IMessage, ulong>, SocketMessage, ISocketMessageChannel> MessageUpdated;
        public event Action<Cacheable<IMessage, ulong>, ISocketMessageChannel> MessageDeleted;

        public MessageUpdateService(IServiceProvider services)
        {
            _discordClient = services.GetRequiredService<DiscordSocketClient>();

            _discordClient.ReactionAdded += ReactionAddedAsync;
            _discordClient.ReactionRemoved += ReactionRemovedAsync;
            _discordClient.MessageUpdated += MessageUpdatedAsync;
            _discordClient.MessageDeleted += MessageDeletedAsync;
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

        private Task MessageUpdatedAsync(Cacheable<IMessage, ulong> before, SocketMessage after, ISocketMessageChannel channel)
        {
            MessageUpdated?.Invoke(before, after, channel);
            return Task.CompletedTask;
        }

        private Task MessageDeletedAsync(Cacheable<IMessage, ulong> message, ISocketMessageChannel channel)
        {
            MessageDeleted?.Invoke(message, channel);
            return Task.CompletedTask;
        }
    }
}
