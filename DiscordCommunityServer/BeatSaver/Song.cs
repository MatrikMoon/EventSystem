using DiscordCommunityShared;
using DiscordCommunityShared.SimpleJSON;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static DiscordCommunityShared.SharedConstructs;

/*
 * Created by Moon on 9/25/2018
 * This class is intended to handle the reading of data from
 * songs downloaded from BeatSaver
 */

namespace DiscordCommunityServer.BeatSaver
{
    class Song
    {
        public static readonly string currentDirectory = Directory.GetCurrentDirectory();
        public static readonly string songDirectory = $@"{currentDirectory}\DownloadedSongs\";

        public LevelDifficulty[] difficulties;
        public string SongName { get; }

        string SongId { get; set; }

        private string _infoPath;

        public Song(string songId)
        {
            SongId = songId;

            if (!OstHelper.IsOst(SongId))
            {
                _infoPath = GetInfoPath();
                difficulties = GetLevelDifficulties();
                SongName = GetSongName();
            }
            else
            {
                SongName = OstHelper.GetOstSongNameFromLevelId(SongId);
                difficulties = OstHelper.GetDifficultiesFromLevelId(songId);
            }
        }

        //Returns the most appropriate LevelDifficulty for the player's rank
        public LevelDifficulty GetDifficultyForRank(Rank rank)
        {
            //TODO: REMOVE
            //Special case for Flower Dance
            if (SongId == "2322-1605")
            {
                LevelDifficulty ret2;
                switch (rank)
                {
                    case Rank.Master:
                    case Rank.Blue:
                    case Rank.Gold:
                    case Rank.Silver:
                        ret2 = GetClosestDifficultyPreferLower(LevelDifficulty.Expert);
                        break;
                    case Rank.Bronze:
                        ret2 = GetClosestDifficultyPreferLower(LevelDifficulty.Hard);
                        break;
                    default:
                        ret2 = GetClosestDifficultyPreferLower(LevelDifficulty.Easy);
                        break;
                }
                return ret2;
            }

            LevelDifficulty ret;
            switch (rank)
            {
                case Rank.Master:
                case Rank.Blue:
                    ret = GetClosestDifficultyPreferLower(LevelDifficulty.ExpertPlus);
                    break;
                case Rank.Gold:
                case Rank.Silver:
                    ret = GetClosestDifficultyPreferLower(LevelDifficulty.Expert);
                    break;
                case Rank.Bronze:
                    ret = GetClosestDifficultyPreferLower(LevelDifficulty.Hard);
                    break;
                default:
                    ret = GetClosestDifficultyPreferLower(LevelDifficulty.Easy);
                    break;
            }
            return ret;
        }

        //Looks at info.json and gets the song name
        private string GetSongName()
        {
            var infoText = File.ReadAllText(_infoPath);
            JSONNode node = JSON.Parse(infoText);
            return node["songName"];
        }

        private LevelDifficulty[] GetLevelDifficulties()
        {
            List<LevelDifficulty> difficulties = new List<LevelDifficulty>();
            var infoText = File.ReadAllText(_infoPath);
            JSONNode node = JSON.Parse(infoText);
            JSONArray difficultyLevels = node["difficultyLevels"].AsArray;
            foreach (var item in difficultyLevels)
            {
                //We can't use DifficultyRank as it uses the same enum value for Expert and E+
                Enum.TryParse(item.Value["difficulty"], out LevelDifficulty difficulty);
                difficulties.Add(difficulty);
            }
            return difficulties.OrderBy(x => x).ToArray();
        }

        //Returns the closest difficulty to the one provided, preferring lower difficulties first if any exist
        private LevelDifficulty GetClosestDifficultyPreferLower(LevelDifficulty difficulty)
        {
            if (difficulties.Contains(difficulty)) return difficulty;

            int ret = -1;
            if (ret == -1)
            {
                ret = GetLowerDifficulty(difficulty);
            }
            if (ret == -1)
            {
                ret = GetHigherDifficulty(difficulty);
            }
            return (LevelDifficulty)ret;
        }

        //Returns the next-lowest difficulty to the one provided
        private int GetLowerDifficulty(LevelDifficulty difficulty)
        {
            return difficulties.Select(x => (int)x).TakeWhile(x => x < (int)difficulty).DefaultIfEmpty(-1).Last();
        }

        //Returns the next-highest difficulty to the one provided
        private int GetHigherDifficulty(LevelDifficulty difficulty)
        {
            return difficulties.Select(x => (int)x).SkipWhile(x => x < (int)difficulty).DefaultIfEmpty(-1).First();
        }

        private string GetInfoPath()
        {
            var songFolder = Directory.GetDirectories($"{songDirectory}{SongId}").First(); //Assuming each id folder has only one song folder
            return Directory.GetFiles(songFolder, "info.json", SearchOption.AllDirectories).First(); //Assuming each song folder has only one info.json
        }
    }
}
