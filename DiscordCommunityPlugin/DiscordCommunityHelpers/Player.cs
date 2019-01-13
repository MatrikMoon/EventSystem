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
        public Team team;

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
                case Rarity.Captain:
                    return Color.red;
                case Rarity.Epic:
                    return Color.magenta;
                case Rarity.Legendary:
                    return Color.yellow;
                case Rarity.Mythic:
                    return Color.cyan;
                case Rarity.Rare:
                    return Color.blue;
                default:
                    return Color.white;
            }
        }

        //Returns the appropriate color for a team
        public Color GetColorForTeam() => GetColorForTeam(team);
        public static Color GetColorForTeam(Team team)
        {
            switch (team)
            {
                case Team.Team1:
                    return Color.red;
                case Team.Team2:
                    return Color.yellow;
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
    }
}
