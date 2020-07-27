using System;
using System.Linq;
using static EventServer.Database.SqlUtils;
using static EventShared.SharedConstructs;

/*
 * Created by Moon on 9/11/2018
 */

namespace EventServer.Database
{
    public class Player
    {
        public string UserId { get; private set; }

        public Player(string userId)
        {
            UserId = userId;
            if (!Exists())
            {
                //Default name is the steam id
                AddPlayer(userId, userId, "", "", "", "-1", 0, 0, 0, 0, 0, 0, 0, true);
            }
        }

        public static Player GetByDiscordMetion(string mention)
        {
            var userId = ExecuteQuery($"SELECT userId FROM playerTable WHERE discordMention = \'{mention}\'", "userId").FirstOrDefault();
            return userId != null ? new Player(userId) : null;
        }

        public string DiscordName
        {
            get
            {
                return ExecuteQuery($"SELECT discordName FROM playerTable WHERE userId = \'{UserId}\'", "discordName").First();
            }
            set
            {
                ExecuteCommand($"UPDATE playerTable SET discordName = \'{value}\' WHERE userId = \'{UserId}\'");
            }
        }

        public string DiscordExtension
        {
            get
            {
                return ExecuteQuery($"SELECT discordExtension FROM playerTable WHERE userId = \'{UserId}\'", "discordExtension").First();
            }
            set
            {
                ExecuteCommand($"UPDATE playerTable SET discordExtension = \'{value}\' WHERE userId = \'{UserId}\'");
            }
        }

        public string DiscordMention
        {
            get
            {
                return ExecuteQuery($"SELECT discordMention FROM playerTable WHERE userId = \'{UserId}\'", "discordMention").First();
            }
            set
            {
                ExecuteCommand($"UPDATE playerTable SET discordMention = \'{value}\' WHERE userId = \'{UserId}\'");
            }
        }

        public string Extras
        {
            get
            {
                return ExecuteQuery($"SELECT extas FROM playerTable WHERE userId = \'{UserId}\'", "extras").First();
            }
            set
            {
                ExecuteCommand($"UPDATE playerTable SET extras = \'{value}\' WHERE userId = \'{UserId}\'");
            }
        }

        public string Team
        {
            get
            {
                return ExecuteQuery($"SELECT team FROM playerTable WHERE userId = \'{UserId}\'", "team").First();
            }
            set
            {
                ExecuteCommand($"UPDATE playerTable SET team = \'{value}\' WHERE userId = \'{UserId}\'");
            }
        }

        public int Rank
        {
            get
            {
                return Convert.ToInt32(ExecuteQuery($"SELECT rank FROM playerTable WHERE userId = \'{UserId}\'", "rank").First());
            }
            set
            {
                ExecuteCommand($"UPDATE playerTable SET rank = {value} WHERE userId = \'{UserId}\'");
            }
        }

        public int Tokens
        {
            get
            {
                return Convert.ToInt32(ExecuteQuery($"SELECT tokens FROM playerTable WHERE userId = \'{UserId}\'", "tokens").First());
            }
            set
            {
                ExecuteCommand($"UPDATE playerTable SET tokens = \'{value}\' WHERE userId = \'{UserId}\'");
            }
        }

        public long TotalScore
        {
            get
            {
                return Convert.ToInt64(ExecuteQuery($"SELECT totalScore FROM playerTable WHERE userId = \'{UserId}\'", "totalScore").First());
            }
            set
            {
                ExecuteCommand($"UPDATE playerTable SET totalScore = {value} WHERE userId = \'{UserId}\'");
            }
        }

        public bool IncrementPersonalBestsBeaten() => ExecuteCommand($"UPDATE playerTable SET personalBestsBeaten = personalBestsBeaten + 1 WHERE userId = \'{UserId}\'") > 1;

        public bool IncrementSongsPlayed() => ExecuteCommand($"UPDATE playerTable SET songsPlayed = songsPlayed + 1 WHERE userId = \'{UserId}\'") > 1;

        public void Liquidate()
        {
            ExecuteCommand($"UPDATE scoreTable SET old = 1 WHERE userId = \'{UserId}\'");
            ExecuteCommand($"UPDATE playerTable SET liquidated = 1 WHERE userId = \'{UserId}\'");
        }

        public bool Exists() => Exists(UserId);

        public static bool Exists(string userId) => ExecuteQuery($"SELECT * FROM playerTable WHERE userId = \'{userId}\'", "userId").Any();

        public static bool IsRegistered(string userId) => ExecuteQuery($"SELECT * FROM playerTable WHERE userId = \'{userId}\' AND NOT liquidated = 1", "discordMention").Any(x => x.Length > 0);

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
            hash = (hash * 7) + UserId.GetHashCode();
            return hash;
        }
        //End necessary overrides
    }
}
