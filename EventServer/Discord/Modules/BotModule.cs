using Discord;
using Discord.Commands;
using Discord.WebSocket;
using EventServer.Database;
using EventServer.Discord.Database;
using EventServer.Discord.Services;
using EventShared;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

/**
 * Created by Moon on 5/18/2019
 * A Discord.NET module for basic bot functionality, not necessarily relating to Beat Saber
 */


namespace EventServer.Discord.Modules
{
    public class BotModule : ModuleBase<SocketCommandContext>
    {
        public ReactionService ReactionService { get; set; }
        public DatabaseService DatabaseService { get; set; }

        //Pull parameters out of an argument list string
        //Note: argument specifiers are required to start with "-"
        private static string ParseArgs(string argString, string argToGet)
        {
            //Return nothing if the parameter arg string is empty
            if (string.IsNullOrWhiteSpace(argString) || string.IsNullOrWhiteSpace(argToGet)) return null;

            List<string> argsWithQuotedStrings = new List<string>();
            string[] argArray = argString.Split(' ');

            for (int x = 0; x < argArray.Length; x++)
            {
                if (argArray[x].StartsWith("\""))
                {
                    string assembledString = string.Empty; //argArray[x].Substring(1) + " ";
                    for (int y = x; y < argArray.Length; y++)
                    {
                        if (argArray[y].StartsWith("\"")) argArray[y] = argArray[y].Substring(1); //Strip quotes off the front of the currently tested word.
                                                                                                  //This is necessary since this part of the code also handles the string right after the open quote
                        if (argArray[y].EndsWith("\""))
                        {
                            assembledString += argArray[y].Substring(0, argArray[y].Length - 1);
                            x = y;
                            break;
                        }
                        else assembledString += argArray[y] + " ";
                    }
                    argsWithQuotedStrings.Add(assembledString);
                }
                else argsWithQuotedStrings.Add(argArray[x]);
            }

            argArray = argsWithQuotedStrings.ToArray();

            for (int i = 0; i < argArray.Length; i++)
            {
                if (argArray[i].ToLower() == $"-{argToGet}".ToLower())
                {
                    if (((i + 1) < (argArray.Length)) && !argArray[i + 1].StartsWith("-"))
                    {
                        return argArray[i + 1];
                    }
                    else return "true";
                }
            }

            return null;
        }

        private bool IsAdmin()
        {
            ulong moderatorRoleId = Context.Guild.Roles.FirstOrDefault(x => CommunityBot.AdminRoles.Contains(x.Name.ToLower())).Id;
            bool isAdmin =
                ((IGuildUser)Context.User).GetPermissions((IGuildChannel)Context.Channel).Has(ChannelPermission.ManageChannels) ||
                ((IGuildUser)Context.User).RoleIds.Any(x => x == moderatorRoleId);
            return isAdmin;
        }

        [Command("test")]
        [Summary("A test command, for quick access to test features")]
        public async Task Test([Remainder] string args = null)
        {
            if (IsAdmin())
            {
                DatabaseService.DatabaseContext.ReactionRoles.RemoveRange(DatabaseService.DatabaseContext.ReactionRoles);
                await DatabaseService.DatabaseContext.SaveChangesAsync();
            }
        }

        [Command("addRoleReaction")]
        [Summary("Creates a message which users can react to to recieve a role")]
        public async Task AddRoleReaction([Remainder] string args = null)
        {
            if (IsAdmin())
            {
                var text = ParseArgs(args, "text");

                var emote = Emote.Parse("<:accepted:579773449590013964>");
                var role = Context.Message.MentionedRoles.FirstOrDefault();
                var channel = Context.Message.MentionedChannels.FirstOrDefault();
                if (role == null || emote == null || channel == null)
                {
                    await ReplyAsync($"{(role == null ? "Role" : "")}{(emote == null ? "Emote" : "")}{(role == null ? "Channel" : "")} not found");
                }
                else
                {
                    var message = await (channel as IMessageChannel).SendMessageAsync(text);

                    var reactionRole = new ReactionRole
                    {
                        GuildId = (long)Context.Guild.Id,
                        MessageId = (long)message.Id,
                        RoleId = (long)role.Id,                    
                        EmojiId = (long)emote.Id
                    };

                    DatabaseService.DatabaseContext.ReactionRoles.Add(reactionRole);
                    await DatabaseService.DatabaseContext.SaveChangesAsync();

                    await message.AddReactionAsync(emote);

                    ReactionService.ReactionAdded += reactionRole.RoleAdded;
                    ReactionService.ReactionRemoved += reactionRole.RoleRemoved;
                }
            }
        }
    }
}
