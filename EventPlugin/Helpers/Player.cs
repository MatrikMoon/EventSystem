using Oculus.Platform;
using Oculus.Platform.Models;
using Steamworks;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static EventShared.SharedConstructs;

/*
 * Created by Moon on 9/14/2018
 * Keeps track of player data and provides relevant helper methods
 * TODO: Add more relevant info later
 */

namespace EventPlugin.Helpers
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

        /*
        //Gets a the player's locally stored score for a map
        public int GetLocalScore(string levelId, LevelDifficulty difficulty, PlayerDataModelSO dataModel = null)
        {
            dataModel = dataModel ?? Resources.FindObjectsOfTypeAll<PlayerDataModelSO>().First();
            var playerLevelStatsData = dataModel.currentLocalPlayer.GetPlayerLevelStatsData(levelId, (BeatmapDifficulty)difficulty);
            return playerLevelStatsData.validScore ? playerLevelStatsData.highScore : 0;
        }
        */

        /*
        //Gets a the player's locally stored rank for a map
        public RankModel.Rank GetLocalRank(string levelId, LevelDifficulty difficulty, PlayerDataModelSO dataModel = null)
        {
            dataModel = dataModel ?? Resources.FindObjectsOfTypeAll<PlayerDataModelSO>().First();
            var playerLevelStatsData = dataModel.currentLocalPlayer.GetPlayerLevelStatsData(levelId, (BeatmapDifficulty)difficulty);
            return playerLevelStatsData.validScore ? playerLevelStatsData.maxRank : RankModel.Rank.E;
        }
        */

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
        public IDifficultyBeatmap GetClosestDifficultyPreferLower(BeatmapLevelSO level, BeatmapDifficulty difficulty, BeatmapCharacteristicSO characteristic = null)
        {
            //First, look at the characteristic parameter. If there's something useful in there, we try to use it, but fall back to Standard
            var desiredCharacteristic = level.beatmapCharacteristics.FirstOrDefault(x => x.serializedName == (characteristic?.serializedName ?? "Standard")) ?? level.beatmapCharacteristics.First();

            IDifficultyBeatmap[] availableMaps =
                level
                .difficultyBeatmapSets
                .FirstOrDefault(x => x.beatmapCharacteristic.serializedName == desiredCharacteristic.serializedName)
                .difficultyBeatmaps
                .OrderBy(x => x.difficulty)
                .ToArray();

            IDifficultyBeatmap ret = availableMaps.FirstOrDefault(x => x.difficulty == difficulty);

            if (ret == null)
            {
                ret = GetLowerDifficulty(availableMaps, difficulty, desiredCharacteristic);
            }
            if (ret == null)
            {
                ret = GetHigherDifficulty(availableMaps, difficulty, desiredCharacteristic);
            }

            return ret;
        }

        //Returns the next-lowest difficulty to the one provided
        private IDifficultyBeatmap GetLowerDifficulty(IDifficultyBeatmap[] availableMaps, BeatmapDifficulty difficulty, BeatmapCharacteristicSO characteristic)
        {
            return availableMaps.TakeWhile(x => x.difficulty < difficulty).LastOrDefault();
        }

        //Returns the next-highest difficulty to the one provided
        private IDifficultyBeatmap GetHigherDifficulty(IDifficultyBeatmap[] availableMaps, BeatmapDifficulty difficulty, BeatmapCharacteristicSO characteristic)
        {
            return availableMaps.SkipWhile(x => x.difficulty < difficulty).FirstOrDefault();
        }

        //User ID code, courtesy of Kyle and Beat Saber Utils//
        public static void UpdateUserId()
        {
            if (VRPlatformHelper.instance.vrPlatformSDK == VRPlatformHelper.VRPlatformSDK.OpenVR || Environment.CommandLine.Contains("-vrmode oculus"))
            {
                GetSteamUser();
            }
            else if (VRPlatformHelper.instance.vrPlatformSDK == VRPlatformHelper.VRPlatformSDK.Oculus)
            {
                GetOculusUser();
            }
            else if (Environment.CommandLine.Contains("fpfc") && VRPlatformHelper.instance.vrPlatformSDK == VRPlatformHelper.VRPlatformSDK.Unknown)
            {
                GetSteamUser();
            }
            else GetSteamUser();
        }

        private static void GetSteamUser()
        {
            if (SteamManager.Initialized)
            {
                Plugin.PlayerId = SteamUser.GetSteamID().m_SteamID;
            }
        }

        private static void GetOculusUser()
        {
            Users.GetLoggedInUser().OnComplete((Message<User> msg) =>
            {
                if (!msg.IsError)
                {
                    Plugin.PlayerId = msg.Data.ID;
                }
            });
        }
    }
}
