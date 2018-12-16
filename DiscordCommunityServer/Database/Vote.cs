﻿using System.Linq;
using static DiscordCommunityShared.SharedConstructs;

namespace DiscordCommunityServer.Database
{
    public class Vote
    {
        public string UserId { get; }
        public string ItemId { get; }

        public Vote(string userId, string itemId, Category category)
        {
            ItemId = itemId;
            UserId = userId;
            if (!Exists()) SimpleSql.AddVote(userId, itemId, category);
        }

        public bool IsOld()
        {
            return SimpleSql.ExecuteQuery($"SELECT old FROM voteTable WHERE itemId = \'{ItemId}\'", "old").First() == "1";
        }

        public bool Exists()
        {
            return Exists(UserId, ItemId);
        }

        public static bool RemoveExsitingVoteForCategory(string userId, Category category)
        {
            return SimpleSql.ExecuteCommand($"DELETE FROM voteTable WHERE userId = \'{userId}\' AND category = \'{(int)category}\'") > 1;
        }

        public static bool Exists(string userId, string itemId)
        {
            return SimpleSql.ExecuteQuery($"SELECT * FROM voteTable WHERE userId = \'{userId}\' AND itemId = \'{itemId}\'", "itemId").Any();
        }
    }
}
