using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using static DiscordCommunityShared.SharedConstructs;

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
        private static Player instance;
        public static Player Instance
        {
            get {
                if (instance == null) return new Player();
                else return instance;
            }
            set {
                instance = value;
            }
        }

        public Rank rank;
        public long tokens;
        public Dictionary<string, GameplayMode> desiredModes;

        public Player()
        {
            Instance = this;
        }

        //Returns the most appropriate map for the player's rank
        public IStandardLevelDifficultyBeatmap GetMapForRank(IStandardLevel level)
        {
            //TODO: REMOVE
            //Special case for Flower Dance
            if (Misc.SongIdHelper.GetSongIdFromLevelId(level.levelID) == "2322-1605")
            {
                IStandardLevelDifficultyBeatmap ret2 = null;
                switch (rank)
                {
                    case Rank.Master:
                    case Rank.Blue:
                    case Rank.Gold:
                    case Rank.Silver:
                        ret2 = GetClosestDifficultyPreferLower(level, LevelDifficulty.Expert);
                        break;
                    case Rank.Bronze:
                        ret2 = GetClosestDifficultyPreferLower(level, LevelDifficulty.Hard);
                        break;
                    default:
                        ret2 = GetClosestDifficultyPreferLower(level, LevelDifficulty.Easy);
                        break;
                }
                return ret2;
            }

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
