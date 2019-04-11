using System;
using System.Collections.Generic;
using System.Linq;
using EventShared;
using static EventServer.Database.SimpleSql;

/*
 * Created by Moon on 9/11/2018
 */

namespace EventServer.Database
{
    public class Player
    {
        public string SteamId { get; private set; }

        public Player(string steamId)
        {
            SteamId = steamId;
            if (!Exists())
            {
                //Default name is the steam id
                AddPlayer(steamId, steamId, "", "", "", (int)SharedConstructs.Rarity.None, "-1", 0, 0, 0, 0, 0, 0, 0, true);
            }
        }

        public static Player GetByDiscordMetion(string mention)
        {
            var steamId = ExecuteQuery($"SELECT steamId FROM playerTable WHERE discordMention = \'{mention}\'", "steamId").FirstOrDefault();
            return steamId != null ? new Player(steamId) : null;
        }

        public string DiscordName
        {
            get
            {
                return ExecuteQuery($"SELECT discordName FROM playerTable WHERE steamId = {SteamId}", "discordName").First();
            }
            set
            {
                ExecuteCommand($"UPDATE playerTable SET discordName = \'{value}\' WHERE steamId = \'{SteamId}\'");
            }
        }

        public string DiscordExtension
        {
            get
            {
                return ExecuteQuery($"SELECT discordExtension FROM playerTable WHERE steamId = {SteamId}", "discordExtension").First();
            }
            set
            {
                ExecuteCommand($"UPDATE playerTable SET discordExtension = \'{value}\' WHERE steamId = \'{SteamId}\'");
            }
        }

        public string DiscordMention
        {
            get
            {
                return ExecuteQuery($"SELECT discordMention FROM playerTable WHERE steamId = {SteamId}", "discordMention").First();
            }
            set
            {
                ExecuteCommand($"UPDATE playerTable SET discordMention = \'{value}\' WHERE steamId = \'{SteamId}\'");
            }
        }

        public string Timezone
        {
            get
            {
                return ExecuteQuery($"SELECT timezone FROM playerTable WHERE steamId = {SteamId}", "timezone").First();
            }
            set
            {
                ExecuteCommand($"UPDATE playerTable SET timezone = \'{value}\' WHERE steamId = \'{SteamId}\'");
            }
        }

        public int Rarity
        {
            get
            {
                return Convert.ToInt32(ExecuteQuery($"SELECT rarity FROM playerTable WHERE steamId = \'{SteamId}\'", "rarity").First());
            }
            set
            {
                ExecuteCommand($"UPDATE playerTable SET rarity = {value} WHERE steamId = \'{SteamId}\'");
            }
        }

        public string Team
        {
            get
            {
                return ExecuteQuery($"SELECT team FROM playerTable WHERE steamId = {SteamId}", "team").First();
            }
            set
            {
                ExecuteCommand($"UPDATE playerTable SET team = \'{value}\' WHERE steamId = \'{SteamId}\'");
            }
        }

        public int Rank
        {
            get
            {
                return Convert.ToInt32(ExecuteQuery($"SELECT rank FROM playerTable WHERE steamId = {SteamId}", "rank").First());
            }
            set
            {
                ExecuteCommand($"UPDATE playerTable SET rank = {value} WHERE steamId = \'{SteamId}\'");
            }
        }

        public int Tokens
        {
            get
            {
                return Convert.ToInt32(ExecuteQuery($"SELECT tokens FROM playerTable WHERE steamId = {SteamId}", "tokens").First());
            }
            set
            {
                ExecuteCommand($"UPDATE playerTable SET tokens = \'{value}\' WHERE steamId = \'{SteamId}\'");
            }
        }

        public long TotalScore
        {
            get
            {
                return Convert.ToInt64(ExecuteQuery($"SELECT totalScore FROM playerTable WHERE steamId = {SteamId}", "totalScore").First());
            }
            set
            {
                ExecuteCommand($"UPDATE playerTable SET totalScore = {value} WHERE steamId = \'{SteamId}\'");
            }
        }

        public bool IncrementPersonalBestsBeaten() => ExecuteCommand($"UPDATE playerTable SET personalBestsBeaten = personalBestsBeaten + 1 WHERE steamId = \'{SteamId}\'") > 1;

        public bool IncrementSongsPlayed() => ExecuteCommand($"UPDATE playerTable SET songsPlayed = songsPlayed + 1 WHERE steamId = \'{SteamId}\'") > 1;

        public static List<string> GetPlayersInRarity(int rarity) => ExecuteQuery($"SELECT steamId FROM playerTable WHERE rarity = {rarity}", "steamId");

        public void Liquidate()
        {
            ExecuteCommand($"UPDATE scoreTable SET old = 1 WHERE steamId = {SteamId}");
            ExecuteCommand($"UPDATE playerTable SET liquidated = 1 WHERE steamId = \'{SteamId}\'");
        }

        public bool Exists() => Exists(SteamId);

        public static bool Exists(string steamId) => ExecuteQuery($"SELECT * FROM playerTable WHERE steamId = {steamId}", "steamId").Any();

        public static bool IsRegistered(string steamId) => ExecuteQuery($"SELECT * FROM playerTable WHERE steamId = \'{steamId}\' AND NOT liquidated = 1", "discordMention").Any(x => x.Length > 0);

        //Necessary overrides for comparison
        public static bool operator ==(Player a, Player b)
        {
            return a.GetHashCode() == b?.GetHashCode();
        }

        public static bool operator !=(Player a, Player b)
        {
            return a.GetHashCode() != b?.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (!(obj is Player)) return false;
            return GetHashCode() == obj.GetHashCode();
        }

        public override int GetHashCode()
        {
            int hash = 13;
            hash = (hash * 7) + SteamId.GetHashCode();
            return hash;
        }
        //End necessary overrides
    }
}
