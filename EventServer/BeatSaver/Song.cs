using EventShared;
using EventShared.SimpleJSON;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EventShared.SharedConstructs;

/*
 * Created by Moon on 9/25/2018
 * This class is intended to handle the reading of data from
 * songs downloaded from BeatSaver
 */

namespace EventServer.BeatSaver
{
    public class Song
    {
        public static readonly string currentDirectory = Directory.GetCurrentDirectory();
        public static readonly string songDirectory = $@"{currentDirectory}\DownloadedSongs\";

        public LevelDifficulty[] Difficulties { get; private set; }
        public string SongName { get; }

        string SongId { get; set; }

        private string _infoPath;

        public Song(string songId)
        {
            SongId = songId;

            if (!OstHelper.IsOst(SongId))
            {
                _infoPath = GetInfoPath();
                Difficulties = GetLevelDifficulties();
                SongName = GetSongName();
            }
            else
            {
                SongName = OstHelper.GetOstSongNameFromLevelId(SongId);
                Difficulties = OstHelper.GetDifficultiesFromLevelId(songId);
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

        public int GetNoteCount(LevelDifficulty difficulty)
        {
            var infoText = File.ReadAllText(GetDifficultyPath(difficulty));
            JSONNode node = JSON.Parse(infoText);
            return node["_notes"].AsArray.Count;
        }

        public int GetMaxScore(LevelDifficulty difficulty)
        {
            int noteCount = GetNoteCount(difficulty);

            //Coptied from game files
            int num = 0;
            int num2 = 1;
            while (num2 < 8)
            {
                if (noteCount >= num2 * 2)
                {
                    num += num2 * num2 * 2 + num2;
                    noteCount -= num2 * 2;
                    num2 *= 2;
                    continue;
                }
                num += num2 * noteCount;
                noteCount = 0;
                break;
            }
            num += noteCount * num2;
            return num * 110;
        }

        //Returns the closest difficulty to the one provided, preferring lower difficulties first if any exist
        public LevelDifficulty GetClosestDifficultyPreferLower(LevelDifficulty difficulty)
        {
            if (Difficulties.Contains(difficulty)) return difficulty;

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
            return Difficulties.Select(x => (int)x).TakeWhile(x => x < (int)difficulty).DefaultIfEmpty(-1).Last();
        }

        //Returns the next-highest difficulty to the one provided
        private int GetHigherDifficulty(LevelDifficulty difficulty)
        {
            return Difficulties.Select(x => (int)x).SkipWhile(x => x < (int)difficulty).DefaultIfEmpty(-1).First();
        }

        private string GetInfoPath()
        {
            var idFolder = $"{songDirectory}{SongId}";
            var songFolder = Directory.GetDirectories(idFolder); //Assuming each id folder has only one song folder
            var subFolder = songFolder.FirstOrDefault() ?? idFolder;
            return Directory.GetFiles(subFolder, "info.json", SearchOption.AllDirectories).First(); //Assuming each song folder has only one info.json
        }

        private string GetDifficultyPath(LevelDifficulty difficulty)
        {
            var idFolder = $"{songDirectory}{SongId}";
            var songFolder = Directory.GetDirectories(idFolder); //Assuming each id folder has only one song folder
            var subFolder = songFolder.FirstOrDefault() ?? idFolder;
            return Directory.GetFiles(subFolder, $"{difficulty}.json", SearchOption.AllDirectories).First(); //Assuming each song folder has only one info.json
        }
    }
}
