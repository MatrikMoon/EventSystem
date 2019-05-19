using System;
using System.Collections.Generic;
using System.Linq;
using EventShared;
using static EventServer.Database.SqlUtils;
using static EventShared.SharedConstructs;

/*
 * Created by Moon on 9/11/2018
 */

namespace EventServer.Database
{
    public class Player
    {
        public string PlayerId { get; private set; }

        public Player(string steamId)
        {
            PlayerId = steamId;
            if (!Exists())
            {
                //Default name is the steam id
                AddPlayer(steamId, steamId, "", "", "", "-1", 0, 0, 0, 0, 0, 0, 0, true);
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
                return ExecuteQuery($"SELECT discordName FROM playerTable WHERE steamId = {PlayerId}", "discordName").First();
            }
            set
            {
                ExecuteCommand($"UPDATE playerTable SET discordName = \'{value}\' WHERE steamId = \'{PlayerId}\'");
            }
        }

        public string DiscordExtension
        {
            get
            {
                return ExecuteQuery($"SELECT discordExtension FROM playerTable WHERE steamId = {PlayerId}", "discordExtension").First();
            }
            set
            {
                ExecuteCommand($"UPDATE playerTable SET discordExtension = \'{value}\' WHERE steamId = \'{PlayerId}\'");
            }
        }

        public string DiscordMention
        {
            get
            {
                return ExecuteQuery($"SELECT discordMention FROM playerTable WHERE steamId = {PlayerId}", "discordMention").First();
            }
            set
            {
                ExecuteCommand($"UPDATE playerTable SET discordMention = \'{value}\' WHERE steamId = \'{PlayerId}\'");
            }
        }

        public string Extras
        {
            get
            {
                return ExecuteQuery($"SELECT extas FROM playerTable WHERE steamId = {PlayerId}", "extras").First();
            }
            set
            {
                ExecuteCommand($"UPDATE playerTable SET extras = \'{value}\' WHERE steamId = \'{PlayerId}\'");
            }
        }

        public string Team
        {
            get
            {
                return ExecuteQuery($"SELECT team FROM playerTable WHERE steamId = {PlayerId}", "team").First();
            }
            set
            {
                ExecuteCommand($"UPDATE playerTable SET team = \'{value}\' WHERE steamId = \'{PlayerId}\'");
            }
        }

        public int Rank
        {
            get
            {
                return Convert.ToInt32(ExecuteQuery($"SELECT rank FROM playerTable WHERE steamId = {PlayerId}", "rank").First());
            }
            set
            {
                ExecuteCommand($"UPDATE playerTable SET rank = {value} WHERE steamId = \'{PlayerId}\'");
            }
        }

        public int Tokens
        {
            get
            {
                return Convert.ToInt32(ExecuteQuery($"SELECT tokens FROM playerTable WHERE steamId = {PlayerId}", "tokens").First());
            }
            set
            {
                ExecuteCommand($"UPDATE playerTable SET tokens = \'{value}\' WHERE steamId = \'{PlayerId}\'");
            }
        }

        public long TotalScore
        {
            get
            {
                return Convert.ToInt64(ExecuteQuery($"SELECT totalScore FROM playerTable WHERE steamId = {PlayerId}", "totalScore").First());
            }
            set
            {
                ExecuteCommand($"UPDATE playerTable SET totalScore = {value} WHERE steamId = \'{PlayerId}\'");
            }
        }

        public bool IncrementPersonalBestsBeaten() => ExecuteCommand($"UPDATE playerTable SET personalBestsBeaten = personalBestsBeaten + 1 WHERE steamId = \'{PlayerId}\'") > 1;

        public bool IncrementSongsPlayed() => ExecuteCommand($"UPDATE playerTable SET songsPlayed = songsPlayed + 1 WHERE steamId = \'{PlayerId}\'") > 1;

        public void Liquidate()
        {
            ExecuteCommand($"UPDATE scoreTable SET old = 1 WHERE steamId = {PlayerId}");
            ExecuteCommand($"UPDATE playerTable SET liquidated = 1 WHERE steamId = \'{PlayerId}\'");
        }

        public bool Exists() => Exists(PlayerId);

        public static bool Exists(string steamId) => ExecuteQuery($"SELECT * FROM playerTable WHERE steamId = {steamId}", "steamId").Any();

        public static bool IsRegistered(string steamId) => ExecuteQuery($"SELECT * FROM playerTable WHERE steamId = \'{steamId}\' AND NOT liquidated = 1", "discordMention").Any(x => x.Length > 0);

        //Returns the difficulty we *should* be using
        public LevelDifficulty GetPreferredDifficulty(bool isOst = false) => GetPreferredDifficulty(Team, isOst);
        public static LevelDifficulty GetPreferredDifficulty(string teamId, bool isOst = false)
        {
            switch (teamId)
            {
                case "master":
                case "blue":
                    return isOst ? LevelDifficulty.Expert : LevelDifficulty.ExpertPlus;
                case "gold":
                case "silver":
                    return LevelDifficulty.Expert;
                case "bronze":
                    return LevelDifficulty.Hard;
                case "white":
                    return LevelDifficulty.Easy;
            }

            return LevelDifficulty.Easy;
        }

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
            hash = (hash * 7) + PlayerId.GetHashCode();
            return hash;
        }
        //End necessary overrides
    }
}
