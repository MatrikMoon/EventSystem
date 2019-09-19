using Discord;
using Discord.WebSocket;
using System.ComponentModel.DataAnnotations;
using System.Data.Linq.Mapping;

/**
 * Created by Moon on 5/18/2019
 * The Model class for a database entry regarding a message which a user can react to for a role
 */

namespace EventServer.Discord.Database
{
    [Table(Name = "ReactionRoles")]
    public class ReactionRole
    {
        [Column(IsPrimaryKey = true, Name = "ID", DbType = "BIGINT", IsDbGenerated = true)]
        [Key]
        public long ID { get; set; }

        [Column(Name = "GuildId")]
        public long GuildId { get; set; }

        [Column(Name = "MessageId")]
        public long MessageId { get; set; }

        [Column(Name = "RoleId")]
        public long RoleId { get; set; }

        [Column(Name = "EmojiId")]
        public long EmojiId { get; set; }

        [Column(Name = "Old")]
        public bool Old { get; set; }

        public void RoleAdded(SocketReaction reaction)
        {
            if (reaction.MessageId == (ulong)MessageId &&
                Emote.TryParse(reaction.Emote.ToString(), out var emote))
            {
                if (emote.Id == (ulong)EmojiId && reaction.Message.IsSpecified && reaction.User.IsSpecified)
                {
                    var guild = (reaction.Channel as SocketGuildChannel).Guild;
                    var user = reaction.User.Value as IGuildUser;
                    var role = guild.GetRole((ulong)RoleId);

                    if (!user.IsBot) user.AddRoleAsync(role);
                }
            }
        }

        public void RoleRemoved(SocketReaction reaction)
        {
            if (reaction.MessageId == (ulong)MessageId &&
                Emote.TryParse(reaction.Emote.ToString(), out var emote))
            {
                if (emote.Id == (ulong)EmojiId && reaction.Message.IsSpecified && reaction.User.IsSpecified)
                {
                    var guild = (reaction.Channel as SocketGuildChannel).Guild;
                    var user = reaction.User.Value as IGuildUser;
                    var role = guild.GetRole((ulong)RoleId);

                    if (!user.IsBot) user.RemoveRoleAsync(role);
                }
            }
        }

        public void MessageDeleted(Cacheable<IMessage, ulong> message, ISocketMessageChannel channel)
        {
            if (message.Id == (ulong)MessageId) Old = true;
        }
    }
}
