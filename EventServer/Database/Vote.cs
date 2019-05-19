using Discord;
using Discord.WebSocket;
using EventServer.Discord;
using EventServer.Discord.Services;
using EventShared;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;

/**
 * Created by Moon on 5/18/2019
 * Represents an active Vote for the blue role in the DiscordCommunity side of things
 * `RegisterVotesWithBot` is called directly from the server's Main to rgister the
 * necessary vote events
 */

namespace EventServer.Database
{
    public class Vote
    {
        public string PlayerId { get; private set; }

        public Vote(string playerId, string ostInfo)
        {
            PlayerId = playerId;
            if (!Exists())
            {
                SqlUtils.AddVote(playerId, 0, "", 0, 0);
                SendVoteMessage(ostInfo);
            }
        }

        private async void SendVoteMessage(string ostInfo)
        {
            var player = new Player(PlayerId);
            var messageText = $"{player.DiscordName} has submitted an application.\n" +
                $"https://scoresaber.com/u/{player.PlayerId}\n\n" +
                ostInfo;

            var message = await CommunityBot.SendToVoteChannel(messageText);
            MessageId = message.Id;
            NextPromotion = "blue";
#if DEBUG
            RequiredVotes = 2;
#else
            RequiredVotes = 8;
#endif

            var reactionService = CommunityBot.GetServices().GetRequiredService<ReactionService>();
            reactionService.ReactionAdded += VoteAdded;
            reactionService.ReactionRemoved += VoteRemoved;
        }

        public ulong MessageId
        {
            get
            {
                var ret = SqlUtils.ExecuteQuery($"SELECT messageId FROM voteTable WHERE playerId = {PlayerId}", "messageId").First();
                return (ulong)Convert.ToInt64(ret);
            }
            set
            {
                SqlUtils.ExecuteCommand($"UPDATE voteTable SET messageId = {value} WHERE playerId = \'{PlayerId}\'");
            }
        }

        public string NextPromotion
        {
            get
            {
                return SqlUtils.ExecuteQuery($"SELECT nextPromotion FROM voteTable WHERE playerId = {PlayerId}", "nextPromotion").First();
            }
            set
            {
                SqlUtils.ExecuteCommand($"UPDATE voteTable SET nextPromotion = \'{value}\' WHERE playerId = \'{PlayerId}\'");
            }
        }

        public int Votes
        {
            get
            {
                var ret = SqlUtils.ExecuteQuery($"SELECT votes FROM voteTable WHERE playerId = {PlayerId}", "votes").First();
                return Convert.ToInt32(ret);
            }
            set
            {
                SqlUtils.ExecuteCommand($"UPDATE voteTable SET votes = {value} WHERE playerId = \'{PlayerId}\'");
            }
        }

        public int RequiredVotes
        {
            get
            {
                var ret = SqlUtils.ExecuteQuery($"SELECT requiredVotes FROM voteTable WHERE playerId = {PlayerId}", "requiredVotes").First();
                return Convert.ToInt32(ret);
            }
            set
            {
                SqlUtils.ExecuteCommand($"UPDATE voteTable SET requiredVotes = {value} WHERE playerId = \'{PlayerId}\'");
            }
        }

        public bool Old
        {
            get
            {
                var ret = SqlUtils.ExecuteQuery($"SELECT old FROM voteTable WHERE playerId = {PlayerId}", "old").First();
                return bool.Parse(ret);
            }
            set
            {
                SqlUtils.ExecuteCommand($"UPDATE voteTable SET old = {(value ? "1" : "0")} WHERE playerId = \'{PlayerId}\'");
            }
        }

        private void PromotePlayer()
        {
            CommunityBot.ChangeTeam(new Player(PlayerId), new Team(NextPromotion));
        }

        private void VoteAdded(SocketReaction reaction)
        {
            if (reaction.MessageId == MessageId && reaction.Emote.Name == "accepted")
            {
                Votes++;
                if (Votes >= RequiredVotes)
                {
                    PromotePlayer();

                    var reactionService = CommunityBot.GetServices().GetRequiredService<ReactionService>();
                    reactionService.ReactionAdded -= VoteAdded;
                    reactionService.ReactionRemoved -= VoteRemoved;
                    Old = true;
                }
            }
        }

        private void VoteRemoved(SocketReaction reaction)
        {
            if (reaction.MessageId == MessageId && reaction.Emote.Name == "accepted")
            {
                Votes--;
            }
        }

        public static void RegisterVotesWithBot() 
        {
            Logger.Info("Registering ongoing votes...");

            var reactionService = CommunityBot.GetServices().GetRequiredService<ReactionService>();
            foreach (var vote in SqlUtils.GetAllVotes())
            {
                Logger.Info($"Registering vote for {new Player(vote.PlayerId).DiscordName}");

                reactionService.ReactionAdded += vote.VoteAdded;
                reactionService.ReactionRemoved += vote.VoteRemoved;
            }
        }

        public bool Exists() => Exists(PlayerId);

        public static bool Exists(string playerId) => SqlUtils.ExecuteQuery($"SELECT * FROM voteTable WHERE playerId = {playerId}", "playerId").Any();
    }
}
