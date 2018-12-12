using DiscordCommunityShared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static DiscordCommunityServer.Database.SimpleSql;
using static DiscordCommunityShared.SharedConstructs;

/*
 * Created by Moon on 9/11/2018
 * TODO: Use Properties (get/set) instead of getters and setters
 */

namespace DiscordCommunityServer.Database
{
    class Player
    {
        //Main SQL identification
        private string playerId;

        public Player(string steamId)
        {
            this.playerId = steamId;
            if (!Exists())
            {
                //Default name is the steam id
                AddPlayer(steamId, steamId, "", "", (int)Category.None, 0, 0, 0, 0, 0, 0, true);
            }
        }

        public static Player GetByDiscord(string mention)
        {
            var steamId = ExecuteQuery($"SELECT steamId FROM playerTable WHERE discordMention = \'{mention}\'", "steamId").FirstOrDefault();
            return steamId != null ? new Player(steamId) : null;
        }

        public string GetPlayerId()
        {
            return playerId;
        }

        public string GetDiscordName()
        {
            return ExecuteQuery($"SELECT discordName FROM playerTable WHERE steamId = {playerId}", "discordName").First();
        }

        public string GetDiscordExtension()
        {
            return ExecuteQuery($"SELECT discordExtension FROM playerTable WHERE steamId = {playerId}", "discordExtension").First();
        }

        public string GetDiscordMention()
        {
            return ExecuteQuery($"SELECT discordMention FROM playerTable WHERE steamId = {playerId}", "discordMention").First();
        }

        public bool SetDiscordName(string discordName)
        {
            return ExecuteCommand($"UPDATE playerTable SET discordName = \'{discordName}\' WHERE steamId = \'{playerId}\'") > 1;
        }

        public bool SetDiscordExtension(string discordExtension)
        {
            return ExecuteCommand($"UPDATE playerTable SET discordExtension = \'{discordExtension}\' WHERE steamId = \'{playerId}\'") > 1;
        }

        public bool SetDiscordMention(string discordMention)
        {
            return ExecuteCommand($"UPDATE playerTable SET discordMention = \'{discordMention}\' WHERE steamId = \'{playerId}\'") > 1;
        }

        public bool Exists()
        {
            return Exists(playerId);
        }

        public static bool Exists(string steamId)
        {
            return ExecuteQuery($"SELECT * FROM playerTable WHERE steamId = {steamId}", "steamId").Any();
        }

        public static bool IsRegistered(string steamId)
        {
            return ExecuteQuery($"SELECT * FROM playerTable WHERE steamId = \'{steamId}\'", "discordMention").Any(x => x.Length > 0);
        }
    }
}
