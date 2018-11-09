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
        private string steamId;

        public Player(string steamId)
        {
            this.steamId = steamId;
            if (!Exists())
            {
                //Default name is the steam id
                AddPlayer(steamId, steamId, "", "", (int)Rank.None, 0, 0, 0, 0, 0, 0, true);
            }
        }

        public static Player GetByDiscord(string mention)
        {
            var steamId = ExecuteQuery($"SELECT steamId FROM playerTable WHERE discordMention = \'{mention}\'", "steamId").FirstOrDefault();
            return steamId != null ? new Player(steamId) : null;
        }

        public string GetSteamId()
        {
            return steamId;
        }

        public string GetDiscordName()
        {
            return ExecuteQuery($"SELECT discordName FROM playerTable WHERE steamId = {steamId}", "discordName").First();
        }

        public string GetDiscordExtension()
        {
            return ExecuteQuery($"SELECT discordExtension FROM playerTable WHERE steamId = {steamId}", "discordExtension").First();
        }

        public string GetDiscordMention()
        {
            return ExecuteQuery($"SELECT discordMention FROM playerTable WHERE steamId = {steamId}", "discordMention").First();
        }

        public bool SetDiscordName(string discordName)
        {
            return ExecuteCommand($"UPDATE playerTable SET discordName = \'{discordName}\' WHERE steamId = \'{steamId}\'") > 1;
        }

        public bool SetDiscordExtension(string discordExtension)
        {
            return ExecuteCommand($"UPDATE playerTable SET discordExtension = \'{discordExtension}\' WHERE steamId = \'{steamId}\'") > 1;
        }

        public bool SetDiscordMention(string discordMention)
        {
            return ExecuteCommand($"UPDATE playerTable SET discordMention = \'{discordMention}\' WHERE steamId = \'{steamId}\'") > 1;
        }

        public int GetRank()
        {
            return Convert.ToInt32(ExecuteQuery($"SELECT rank FROM playerTable WHERE steamId = \'{steamId}\'", "rank").First());
        }

        public bool SetRank(int rank)
        {
            return ExecuteCommand($"UPDATE playerTable SET rank = {rank} WHERE steamId = \'{steamId}\'") > 1;
        }

        public int GetTokens()
        {
            return Convert.ToInt32(ExecuteQuery($"SELECT tokens FROM playerTable WHERE steamId = {steamId}", "tokens").First());
        }

        public bool SetTokens(int tokens)
        {
            return ExecuteCommand($"UPDATE playerTable SET tokens = {tokens} WHERE steamId = \'{steamId}\'") > 1;
        }

        public int GetTotalScore()
        {
            return Convert.ToInt32(ExecuteQuery($"SELECT totalScore FROM playerTable WHERE steamId = {steamId}", "totalScore").First());
        }

        public bool IncrementTotalScore(long scoreToAdd)
        {
            return ExecuteCommand($"UPDATE playerTable SET totalScore = totalScore + {scoreToAdd} WHERE steamId = \'{steamId}\'") > 1;
        }

        public bool IncrementPersonalBestsBeaten()
        {
            return ExecuteCommand($"UPDATE playerTable SET personalBestsBeaten = personalBestsBeaten + 1 WHERE steamId = \'{steamId}\'") > 1;
        }

        public bool IncrementSongsPlayed()
        {
            return ExecuteCommand($"UPDATE playerTable SET songsPlayed = songsPlayed + 1 WHERE steamId = \'{steamId}\'") > 1;
        }

        //Returns the amount of tokens the user would get if scores were calculated now
        public int GetProjectedTokens()
        {
            int rank = GetRank();
            if (rank != (int)Rank.Master)
            {
                int tokens = 0;

                IDictionary<SongConstruct, ScoreConstruct> personalScores = GetScoresForPlayer(steamId);

                IDictionary<SongConstruct, IDictionary<string, ScoreConstruct>> rankAboveScores = GetAllActiveScoresForRank((SharedConstructs.Rank)rank + 1);

                personalScores.ToList().ForEach(x =>
                {
                    IDictionary<string, ScoreConstruct> rankAboveForSong = rankAboveScores[x.Key];
                    if (rankAboveForSong != null) tokens += rankAboveForSong.Where(y => y.Value.Score < x.Value.Score).Count();
                });

                return tokens > 3 ? 3 : tokens; //Cap at 3 tokens
            }
            return 0;
        }

        public static List<string> GetPlayersInRank(int rank)
        {
            return ExecuteQuery($"SELECT steamId FROM playerTable WHERE rank = {rank}", "steamId");
        }

        public bool Exists()
        {
            return Exists(steamId);
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
