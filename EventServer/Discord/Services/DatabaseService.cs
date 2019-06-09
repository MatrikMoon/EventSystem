using EventServer.Discord.Database;
using EventShared;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Data.Entity;

/**
 * Created by Moon on 5/18/2019
 * A service for interfacing with a EF database
 */

namespace EventServer.Discord.Services
{
    public class DatabaseService
    {
        public DatabaseContext DatabaseContext { get; private set; }

        private MessageUpdateService _messageUpdateService;

        public DatabaseService(string location, IServiceProvider serviceProvider)
        {
            DatabaseContext = new DatabaseContext(location);

            _messageUpdateService = serviceProvider.GetRequiredService<MessageUpdateService>();
        }

        public void RegisterReactionRolesWithBot()
        {
            Logger.Info("Registering saved ReactionRoles...");

            foreach (var reactionRole in DatabaseContext.ReactionRoles)
            {
                if (!reactionRole.Old)
                {
                    Logger.Info($"Registering ReactionRole for {reactionRole.ID} {reactionRole.MessageId} {reactionRole.RoleId} {reactionRole.EmojiId}");

                    _messageUpdateService.ReactionAdded += reactionRole.RoleAdded;
                    _messageUpdateService.ReactionRemoved += reactionRole.RoleRemoved;
                    _messageUpdateService.MessageDeleted += reactionRole.MessageDeleted;
                }
            }
        }
    }
}
