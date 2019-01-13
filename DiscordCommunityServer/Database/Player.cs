using System;
using System.Collections.Generic;
using System.Linq;
using static TeamSaberServer.Database.SimpleSql;
using static TeamSaberShared.SharedConstructs;

/*
 * Created by Moon on 9/11/2018
 * TODO: Use Properties (get/set) instead of getters and setters
 */

namespace TeamSaberServer.Database
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
                AddPlayer(steamId, steamId, "", "", "", (int)Rarity.None, (int)Team.None, 0, 0, 0, 0, 0, true);
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

        public string GetTimezone()
        {
            return ExecuteQuery($"SELECT timezone FROM playerTable WHERE steamId = {steamId}", "timezone").First();
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

        public bool SetTimezone(string timezone)
        {
            return ExecuteCommand($"UPDATE playerTable SET timezone = \'{timezone}\' WHERE steamId = \'{steamId}\'") > 1;
        }

        public int GetRarity()
        {
            return Convert.ToInt32(ExecuteQuery($"SELECT rarity FROM playerTable WHERE steamId = \'{steamId}\'", "rarity").First());
        }

        public bool SetRarity(int rarity)
        {
            return ExecuteCommand($"UPDATE playerTable SET rarity = {rarity} WHERE steamId = \'{steamId}\'") > 1;
        }

        public int GetTeam()
        {
            return Convert.ToInt32(ExecuteQuery($"SELECT team FROM playerTable WHERE steamId = {steamId}", "team").First());
        }

        public bool SetTeam(int team)
        {
            return ExecuteCommand($"UPDATE playerTable SET team = {team} WHERE steamId = \'{steamId}\'") > 1;
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

        public static List<string> GetPlayersInRarity(int rarity)
        {
            return ExecuteQuery($"SELECT steamId FROM playerTable WHERE rarity = {rarity}", "steamId");
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
