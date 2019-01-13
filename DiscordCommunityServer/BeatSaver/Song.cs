using TeamSaberShared;
using TeamSaberShared.SimpleJSON;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static TeamSaberShared.SharedConstructs;

/*
 * Created by Moon on 9/25/2018
 * This class is intended to handle the reading of data from
 * songs downloaded from BeatSaver
 */

namespace TeamSaberServer.BeatSaver
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
