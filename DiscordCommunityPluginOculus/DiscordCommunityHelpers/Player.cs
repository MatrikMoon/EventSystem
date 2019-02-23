using Oculus.Platform;
using Oculus.Platform.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static TeamSaberShared.SharedConstructs;
using Logger = TeamSaberShared.Logger;

/*
 * Created by Moon on 9/14/2018
 * Keeps track of player data and provides relevant helper methods
 * TODO: Add more relevant info later
 */

namespace TeamSaberPlugin.DiscordCommunityHelpers
{
    [Obfuscation(Exclude = false, Feature = "+rename(mode=decodable,renPdb=true)")]
    class Player
    {
        //Instance
        private static Player instance;
        public static Player Instance
        {
            get {
                return instance ?? new Player();
            }
            set {
                instance = value;
            }
        }

        //Fields
        public Rarity rarity;
        public string team;

        //Constructor
        public Player()
        {
            Instance = this;
        }

        //Gets a the player's locally stored score for a map
        public int GetLocalScore(string levelId, LevelDifficulty difficulty, PlayerDataModelSO dataModel = null)
        {
            dataModel = dataModel ?? Resources.FindObjectsOfTypeAll<PlayerDataModelSO>().First();
            var playerLevelStatsData = dataModel.currentLocalPlayer.GetPlayerLevelStatsData(levelId, (BeatmapDifficulty)difficulty);
            return playerLevelStatsData.validScore ? playerLevelStatsData.highScore : 0;
        }

        //Gets a the player's locally stored rank for a map
        public RankModel.Rank GetLocalRank(string levelId, LevelDifficulty difficulty, PlayerDataModelSO dataModel = null)
        {
            dataModel = dataModel ?? Resources.FindObjectsOfTypeAll<PlayerDataModelSO>().First();
            var playerLevelStatsData = dataModel.currentLocalPlayer.GetPlayerLevelStatsData(levelId, (BeatmapDifficulty)difficulty);
            return playerLevelStatsData.validScore ? playerLevelStatsData.maxRank : RankModel.Rank.E;
        }

        //Returns the appropriate color for a rarity
        public Color GetColorForRarity() => GetColorForRarity(rarity);
        public static Color GetColorForRarity(Rarity rarity)
        {
            switch (rarity)
            {
                case Rarity.SSS:
                    return Color.red;
                case Rarity.A:
                    return Color.magenta;
                case Rarity.S:
                    return Color.yellow;
                case Rarity.SS:
                    return Color.cyan;
                case Rarity.B:
                    return Color.blue;
                default:
                    return Color.white;
            }
        }

        //Returns the closest difficulty to the one provided, preferring lower difficulties first if any exist
        public IDifficultyBeatmap GetClosestDifficultyPreferLower(IBeatmapLevel level, BeatmapDifficulty difficulty)
        {
            IDifficultyBeatmap ret = level.GetDifficultyBeatmap(difficulty);
            if (ret == null)
            {
                ret = GetLowerDifficulty(level, difficulty);
            }
            if (ret == null)
            {
                ret = GetHigherDifficulty(level, difficulty);
            }
            return ret;
        }

        //Returns the next-lowest difficulty to the one provided
        private IDifficultyBeatmap GetLowerDifficulty(IBeatmapLevel level, BeatmapDifficulty difficulty)
        {
            IDifficultyBeatmap[] availableMaps = level.difficultyBeatmaps.OrderBy(x => x.difficulty).ToArray();
            return availableMaps.TakeWhile(x => x.difficulty < difficulty).LastOrDefault();
        }

        //Returns the next-highest difficulty to the one provided
        private IDifficultyBeatmap GetHigherDifficulty(IBeatmapLevel level, BeatmapDifficulty difficulty)
        {
            IDifficultyBeatmap[] availableMaps = level.difficultyBeatmaps.OrderBy(x => x.difficulty).ToArray();
            return availableMaps.SkipWhile(x => x.difficulty < difficulty).FirstOrDefault();
        }

        //User ID code, courtesy of Kyle and Beat Saber Utils//
        public static ulong GetUserId()
        {
            if (VRPlatformHelper.instance.vrPlatformSDK == VRPlatformHelper.VRPlatformSDK.OpenVR || Environment.CommandLine.Contains("-vrmode oculus"))
            {
                return GetSteamUser();
            }
            else if (VRPlatformHelper.instance.vrPlatformSDK == VRPlatformHelper.VRPlatformSDK.Oculus)
            {
                return GetOculusUser();
            }
            else if (Environment.CommandLine.Contains("fpfc") && VRPlatformHelper.instance.vrPlatformSDK == VRPlatformHelper.VRPlatformSDK.Unknown)
            {
                return GetSteamUser();
            }
            else return 0;
        }

        internal static ulong GetSteamUser()
        {
            return 0;
        }

        internal static ulong GetOculusUser()
        {
            ulong ret = 0;
            Users.GetLoggedInUser().OnComplete((Message<User> msg) =>
            {
                if (!msg.IsError)
                {
                    ret = msg.Data.ID;
                }
            });

            while (ret == 0) { } //TODO: Gross. Shame on you, Moon.

            return ret;
        }
    }
}
