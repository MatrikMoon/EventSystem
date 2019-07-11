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

namespace EventPlugin.Models
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
        public string Team { get; set; }
        public int Tokens { get; set; }
        public ServerFlags ServerOptions { get; set; }

        //Constructor
        public Player()
        {
            Instance = this;
        }

        //Determines whether or not the player qualifies for ranking up
        public bool CanRankUp()
        {
            var currentTeam = Models.Team.allTeams.First(x => x.TeamId == Team);
            var nextTeam = Models.Team.allTeams.First(x => x.TeamId == currentTeam.NextPromotion);
            if (Tokens < nextTeam.RequiredTokens) return false;

            //return GetSongsToImproveBeforeRankUp(rank).Count <= 0;
            return true;
        }

        //Gets a the player's locally stored score for a map
        public int GetLocalScore(IDifficultyBeatmap map, PlayerDataModelSO dataModel = null)
        {
            dataModel = dataModel ?? Resources.FindObjectsOfTypeAll<PlayerDataModelSO>().First();
            var playerLevelStatsData = dataModel.currentLocalPlayer.GetPlayerLevelStatsData(map.level.levelID, map.difficulty, map.parentDifficultyBeatmapSet.beatmapCharacteristic);
            return playerLevelStatsData.validScore ? playerLevelStatsData.highScore : 0;
        }

        //Gets a the player's locally stored rank for a map
        public RankModel.Rank GetLocalRank(IDifficultyBeatmap map, PlayerDataModelSO dataModel = null)
        {
            dataModel = dataModel ?? Resources.FindObjectsOfTypeAll<PlayerDataModelSO>().First();
            var playerLevelStatsData = dataModel.currentLocalPlayer.GetPlayerLevelStatsData(map.level.levelID, map.difficulty, map.parentDifficultyBeatmapSet.beatmapCharacteristic);
            return playerLevelStatsData.validScore ? playerLevelStatsData.maxRank : RankModel.Rank.E;
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
                Plugin.UserId = SteamUser.GetSteamID().m_SteamID;
            }
        }

        private static void GetOculusUser()
        {
            Users.GetLoggedInUser().OnComplete((Message<User> msg) =>
            {
                if (!msg.IsError)
                {
                    Plugin.UserId = msg.Data.ID;
                }
            });
        }
    }
}
