using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static DiscordCommunityShared.SharedConstructs;
using Logger = DiscordCommunityShared.Logger;

/*
 * Created by Moon on 9/14/2018
 * Keeps track of player data and provides relevant helper methods
 * TODO: Add more relevant info later
 */

namespace DiscordCommunityPlugin.DiscordCommunityHelpers
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
        public Rank rank;
        public long tokens;
        public long projectedTokens;
        public Dictionary<string, GameplayMode> desiredModes;

        //Constructor
        public Player()
        {
            Instance = this;
        }

        private class OSTScoreRequirement
        {
            public LevelCompletionResults.Rank Rank { get; set; }
            public LevelDifficulty Difficulty { get; set; }
        }

        private static readonly OSTScoreRequirement[] bronzeRequirements = {
            new OSTScoreRequirement {
                Rank = LevelCompletionResults.Rank.SS,
                Difficulty = LevelDifficulty.Normal
            },
            new OSTScoreRequirement {
                Rank = LevelCompletionResults.Rank.A,
                Difficulty = LevelDifficulty.Hard
            }
        };

        private static readonly OSTScoreRequirement[] silverRequirements = {
            new OSTScoreRequirement {
                Rank = LevelCompletionResults.Rank.S,
                Difficulty = LevelDifficulty.Hard
            },
            new OSTScoreRequirement {
                Rank = LevelCompletionResults.Rank.A,
                Difficulty = LevelDifficulty.Expert
            }
        };

        private static readonly OSTScoreRequirement[] goldRequirements = {
            new OSTScoreRequirement {
                Rank = LevelCompletionResults.Rank.SS,
                Difficulty = LevelDifficulty.Hard
            },
            new OSTScoreRequirement {
                Rank = LevelCompletionResults.Rank.S,
                Difficulty = LevelDifficulty.Expert
            }
        };

        private static readonly OSTScoreRequirement[] blueRequirements = {
            new OSTScoreRequirement {
                Rank = LevelCompletionResults.Rank.SS,
                Difficulty = LevelDifficulty.Expert
            }
        };

        //Determines whether or not the player qualifies for ranking up
        public bool CanRankUp()
        {
            if (rank >= Rank.Gold && tokens < 3) return false;
            return GetSongsToImproveBeforeRankUp(rank).Count <= 0;
        }
        
        //Returns the rank (between white and gold, inclusive) that suits the current player's scores
        public Rank GetSuitableRank()
        {
            Rank[] ranks = { Rank.White, Rank.Bronze, Rank.Silver };
            Rank lastSuitableRank = Rank.White;
            foreach (Rank rank in ranks)
            {
                if (GetSongsToImproveBeforeRankUp(rank).Count == 0) lastSuitableRank = rank + 1;
            }
            return lastSuitableRank;
        }

        //Returns a list of songs (levelIds) the player needs to improve before ranking up
        public List<string> GetSongsToImproveBeforeRankUp() => GetSongsToImproveBeforeRankUp(rank);
        public List<string> GetSongsToImproveBeforeRankUp(Rank currentRank)
        {
            List<string> ret = new List<string>();

            //Get the current applicable requirement set
            OSTScoreRequirement[] requirements = null;
            switch (currentRank)
            {
                case Rank.White:
                    requirements = bronzeRequirements;
                    break;
                case Rank.Bronze:
                    requirements = silverRequirements;
                    break;
                case Rank.Silver:
                    requirements = goldRequirements;
                    break;
                case Rank.Gold:
                    requirements = blueRequirements;
                    break;
                default:
                    break;
            }
            if (requirements == null) return ret; //If none exists, allow rank up (should only happen if current is Rank.None)


            DiscordCommunityShared.OstHelper.ostHashes
                .Take(10) //Not Angel Voices
                .ToList()
                .ForEach(x =>
            {
                //TODO: This is gross. There's gotta be a more sexy way
                bool passedOne = false;
                requirements.ToList().ForEach(y =>
                {
                    if (GetLocalRank(x, y.Difficulty, GameplayMode.SoloStandard) >= y.Rank) passedOne = true;
                });
                if (!passedOne) ret.Add(x);
            });

            return ret;
        }

        //Gets a the player's locally stored score for a map
        public int GetLocalScore(string levelId, LevelDifficulty difficulty, GameplayMode mode)
        {
            var playerLevelStatsData = PersistentSingleton<GameDataModel>.instance.gameDynamicData.GetCurrentPlayerDynamicData().GetPlayerLevelStatsData(levelId, difficulty, mode);
            return playerLevelStatsData.validScore ? playerLevelStatsData.highScore : 0;
        }

        //Gets a the player's locally stored rank for a map
        public LevelCompletionResults.Rank GetLocalRank(string levelId, LevelDifficulty difficulty, GameplayMode mode)
        {
            var playerLevelStatsData = PersistentSingleton<GameDataModel>.instance.gameDynamicData.GetCurrentPlayerDynamicData().GetPlayerLevelStatsData(levelId, difficulty, mode);
            return playerLevelStatsData.validScore ? playerLevelStatsData.maxRank : LevelCompletionResults.Rank.E;
        }

        //Returns the appropriate color for a rank
        public Color GetColorForRank() => GetColorForRank(rank);
        public static Color GetColorForRank(Rank rank)
        {
            switch (rank)
            {
                case Rank.Master:
                    return Color.magenta;
                case Rank.Blue:
                    return Color.blue;
                case Rank.Gold:
                    return Color.yellow;
                case Rank.Silver:
                    return Color.gray;
                default:
                    return Color.white;
            }
        }

        //Returns the most appropriate map for the player's rank
        public IStandardLevelDifficultyBeatmap GetMapForRank(IStandardLevel level)
        {
            IStandardLevelDifficultyBeatmap ret = null;
            switch (rank)
            {
                case Rank.Master:
                case Rank.Blue:
                    ret = GetClosestDifficultyPreferLower(level, LevelDifficulty.ExpertPlus);
                    break;
                case Rank.Gold:
                case Rank.Silver:
                    ret = GetClosestDifficultyPreferLower(level, LevelDifficulty.Expert);
                    break;
                case Rank.Bronze:
                    ret = GetClosestDifficultyPreferLower(level, LevelDifficulty.Hard);
                    break;
                default:
                    ret = GetClosestDifficultyPreferLower(level, LevelDifficulty.Easy);
                    break;
            }
            return ret;
        }

        //Returns the closest difficulty to the one provided, preferring lower difficulties first if any exist
        private IStandardLevelDifficultyBeatmap GetClosestDifficultyPreferLower(IStandardLevel level, LevelDifficulty difficulty)
        {
            IStandardLevelDifficultyBeatmap ret = level.GetDifficultyLevel(difficulty);
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
        private IStandardLevelDifficultyBeatmap GetLowerDifficulty(IStandardLevel level, LevelDifficulty difficulty)
        {
            IStandardLevelDifficultyBeatmap[] availableMaps = level.difficultyBeatmaps.OrderBy(x => x.difficulty).ToArray();
            return availableMaps.TakeWhile(x => x.difficulty < difficulty).LastOrDefault();
        }

        //Returns the next-highest difficulty to the one provided
        private IStandardLevelDifficultyBeatmap GetHigherDifficulty(IStandardLevel level, LevelDifficulty difficulty)
        {
            IStandardLevelDifficultyBeatmap[] availableMaps = level.difficultyBeatmaps.OrderBy(x => x.difficulty).ToArray();
            return availableMaps.SkipWhile(x => x.difficulty < difficulty).FirstOrDefault();
        }
    }
}
