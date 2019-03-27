using TeamSaberShared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static TeamSaberShared.SharedConstructs;
using System.Text.RegularExpressions;

/*
 * Created by Moon on 9/11/2018
 * TODO: Formal identification for songs, not reliant upon some (but not other) properties of the song
 */

namespace EventServer.Database
{
    class Song
    {
        public string SongId { get; private set; }
        public string SongName
        {
            get => SimpleSql.ExecuteQuery($"SELECT songName FROM songTable WHERE songId = \'{SongId}\' AND difficulty = {(int)Difficulty}", "songName").First();
            set => SimpleSql.ExecuteCommand($"UPDATE songTable SET songName = \'{value}\' WHERE songId = \'{SongId}\' AND difficulty = {(int)Difficulty}");
        }
        public int GameOptions
        {
            get
            {
                var optionString = SimpleSql.ExecuteQuery($"SELECT gameOptions FROM songTable WHERE songId = \'{SongId}\' AND difficulty = {(int)Difficulty}", "gameOptions").First();
                return Convert.ToInt32(optionString);
            }
            set => SimpleSql.ExecuteCommand($"UPDATE songTable SET gameOptions = {value} WHERE songId = \'{SongId}\' AND difficulty = {(int)Difficulty}");
        }
        public int PlayerOptions
        {
            get
            {
                var optionString = SimpleSql.ExecuteQuery($"SELECT playerOptions FROM songTable WHERE songId = \'{SongId}\' AND difficulty = {(int)Difficulty}", "playerOptions").First();
                return Convert.ToInt32(optionString);
            }
            set => SimpleSql.ExecuteCommand($"UPDATE songTable SET playerOptions = {value} WHERE songId = \'{SongId}\' AND difficulty = {(int)Difficulty}");
        }
        public bool Old
        {
            get => SimpleSql.ExecuteQuery($"SELECT old FROM songTable WHERE songId = \'{SongId}\' AND difficulty = {(int)Difficulty}", "old").First() == "1";
            set => SimpleSql.ExecuteCommand($"UPDATE songTable SET old = \'{(value ? "1" : "0")}\' WHERE songId = \'{SongId}\' AND difficulty = {(int)Difficulty}");
        }
        public LevelDifficulty Difficulty { get; private set; }

        public Song(string songId, LevelDifficulty difficulty)
        {
            SongId = songId;
            Difficulty = difficulty;
            if (!Exists())
            {
                //Add a placeholder, trigger song download from BeatSaver if it doesn't exist
                SimpleSql.AddSong("", "", "", songId, difficulty, SharedConstructs.PlayerOptions.None, SharedConstructs.GameOptions.None);
                if (OstHelper.IsOst(songId))
                {
                    string songName = OstHelper.GetOstSongNameFromLevelId(songId);
                    SongName = Regex.Replace(songName, "[^a-zA-Z0-9- ]", "");
                }
                else BeatSaver.BeatSaverDownloader.DownloadSongInfoThreaded(SongId, (b) =>
                {
                    if (b)
                    {
                        string songName = new BeatSaver.Song(SongId).SongName;
                        SongName = Regex.Replace(songName, "[^a-zA-Z0-9- ]", "");
                    }
                    else SongName = "[Could not download song info]";
                });
            }
        }

        public bool Exists()
        {
            return Exists(SongId, Difficulty);
        }

        public static bool Exists(string songId, LevelDifficulty difficulty)
        {
            return SimpleSql.ExecuteQuery($"SELECT * FROM songTable WHERE songId = \'{songId}\' AND difficulty = {(int)difficulty} AND old = 0", "songId").Any();
        }
    }
}
