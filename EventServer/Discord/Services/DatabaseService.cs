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

        private ReactionService _reactionService;

        public DatabaseService(string location, IServiceProvider serviceProvider)
        {
            DatabaseContext = new DatabaseContext(location);

            _reactionService = serviceProvider.GetRequiredService<ReactionService>();
        }

        public void RegisterReactionRolesWithBot()
        {
            Logger.Info("Registering saved ReactionRoles...");

            foreach (var reactionRole in DatabaseContext.ReactionRoles)
            {
                Logger.Info($"Registering ReactionRole for {reactionRole.ID} {reactionRole.MessageId} {reactionRole.RoleId} {reactionRole.EmojiId}");

                _reactionService.ReactionAdded += reactionRole.RoleAdded;
                _reactionService.ReactionRemoved += reactionRole.RoleRemoved;
            }
        }
    }
}