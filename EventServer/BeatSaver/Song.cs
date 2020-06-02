using EventShared;
using EventShared.SimpleJSON;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        public string[] Characteristics { get; private set; }
        public string SongName { get; }

        string SongHash { get; set; }

        private string _infoPath;

        public Song(string songHash)
        {
            SongHash = songHash;

            if (!OstHelper.IsOst(SongHash))
            {
                _infoPath = GetInfoPath();
                Characteristics = GetBeatmapCharacteristics();
                SongName = GetSongName();
            }
            else
            {
                SongName = OstHelper.GetOstSongNameFromLevelId(SongHash);
                Characteristics = new string[] { "Standard", "OneSaber", "NoArrows", "90Degree", "360Degree" };
            }
        }

        //Looks at info.json and gets the song name
        private string GetSongName()
        {
            var infoText = File.ReadAllText(_infoPath);
            JSONNode node = JSON.Parse(infoText);
            return node["_songName"];
        }

        private string[] GetBeatmapCharacteristics()
        {
            List<string> characteristics = new List<string>();
            var infoText = File.ReadAllText(_infoPath);
            JSONNode node = JSON.Parse(infoText);
            JSONArray difficultyBeatmapSets = node["_difficultyBeatmapSets"].AsArray;
            foreach (var item in difficultyBeatmapSets)
            {
                characteristics.Add(item.Value["_beatmapCharacteristicName"]);
            }
            return characteristics.OrderBy(x => x).ToArray();
        }

        public LevelDifficulty[] GetLevelDifficulties(string characteristicId)
        {
            List<LevelDifficulty> difficulties = new List<LevelDifficulty>();
            var infoText = File.ReadAllText(_infoPath);
            JSONNode node = JSON.Parse(infoText);
            JSONArray difficultyBeatmapSets = node["_difficultyBeatmapSets"].AsArray;
            var difficultySet = difficultyBeatmapSets.Linq.First(x => x.Value["_beatmapCharacteristicName"] == characteristicId).Value;
            var difficultyBeatmaps = difficultySet["_difficultyBeatmaps"].AsArray;

            foreach (var item in difficultyBeatmaps)
            {
                Enum.TryParse(item.Value["_difficulty"], out LevelDifficulty difficulty);
                difficulties.Add(difficulty);
            }
            return difficulties.OrderBy(x => x).ToArray();
        }

        public string GetPathForDifficulty(string characteristicId, LevelDifficulty difficulty)
        {
            List<LevelDifficulty> difficulties = new List<LevelDifficulty>();
            var infoText = File.ReadAllText(_infoPath);
            JSONNode node = JSON.Parse(infoText);
            JSONArray difficultyBeatmapSets = node["_difficultyBeatmapSets"].AsArray;
            var difficultySet = difficultyBeatmapSets.Linq.First(x => x.Value["_beatmapCharacteristicName"] == characteristicId).Value;
            var difficultyBeatmap = difficultySet["_difficultyBeatmaps"].Linq.First(x => x.Value["_difficulty"].Value == difficulty.ToString()).Value;
            var fileName = difficultyBeatmap["_beatmapFilename"].Value;

            var idFolder = $"{songDirectory}{SongHash}";
            var songFolder = Directory.GetDirectories(idFolder); //Assuming each id folder has only one song folder
            var subFolder = songFolder.FirstOrDefault() ?? idFolder;
            return Directory.GetFiles(subFolder, fileName, SearchOption.AllDirectories).First(); //Assuming each song folder has only one info.json
        }

        public int GetNoteCount(string characteristicId, LevelDifficulty difficulty)
        {
            var infoText = File.ReadAllText(GetPathForDifficulty(characteristicId, difficulty));
            JSONNode node = JSON.Parse(infoText);
            return node["_notes"].AsArray.Count;
        }

        public int GetMaxScore(string characteristicId, LevelDifficulty difficulty)
        {
            int noteCount = GetNoteCount(characteristicId, difficulty);

            //Copied from game files
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
            return num * 115;
        }

        //Returns the closest difficulty to the one provided, preferring lower difficulties first if any exist
        public LevelDifficulty GetClosestDifficultyPreferLower(LevelDifficulty difficulty) => GetClosestDifficultyPreferLower("Standard", difficulty);
        public LevelDifficulty GetClosestDifficultyPreferLower(string characteristicId, LevelDifficulty difficulty)
        {
            if (GetLevelDifficulties(characteristicId).Contains(difficulty)) return difficulty;

            int ret = -1;
            if (ret == -1)
            {
                ret = GetLowerDifficulty(characteristicId, difficulty);
            }
            if (ret == -1)
            {
                ret = GetHigherDifficulty(characteristicId, difficulty);
            }
            return (LevelDifficulty)ret;
        }

        //Returns the next-lowest difficulty to the one provided
        private int GetLowerDifficulty(string characteristicId, LevelDifficulty difficulty)
        {
            return GetLevelDifficulties(characteristicId).Select(x => (int)x).TakeWhile(x => x < (int)difficulty).DefaultIfEmpty(-1).Last();
        }

        //Returns the next-highest difficulty to the one provided
        private int GetHigherDifficulty(string characteristicId, LevelDifficulty difficulty)
        {
            return GetLevelDifficulties(characteristicId).Select(x => (int)x).SkipWhile(x => x < (int)difficulty).DefaultIfEmpty(-1).First();
        }

        private string GetInfoPath()
        {
            var idFolder = $"{songDirectory}{SongHash}";
            var songFolder = Directory.GetDirectories(idFolder); //Assuming each id folder has only one song folder
            var subFolder = songFolder.FirstOrDefault() ?? idFolder;
            return Directory.GetFiles(subFolder, "info.dat", SearchOption.AllDirectories).First(); //Assuming each song folder has only one info.json
        }

        public static bool Exists(string hash)
        {
            return Directory.Exists($"{songDirectory}{hash}");
        }
    }
}
