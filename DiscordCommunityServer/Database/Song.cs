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
 * TODO: Use Properties (get/set) instead of getters and setters
 * TODO: Formal identification for songs, not reliant upon some (but not other) properties of the song
 */

namespace TeamSaberServer.Database
{
    class Song
    {
        private string songId;
        private LevelDifficulty difficulty;

        public Song(string songId, LevelDifficulty difficulty)
        {
            this.songId = songId;
            this.difficulty = difficulty;
            if (!Exists())
            {
                //Add a placeholder, trigger song download from BeatSaver if it doesn't exist
                SimpleSql.AddSong("", "", "", songId, difficulty, PlayerOptions.None, GameOptions.None);
                if (OstHelper.IsOst(songId))
                {
                    SetSongName(OstHelper.GetOstSongNameFromLevelId(songId));
                }
                else BeatSaver.BeatSaverDownloader.DownloadSongInfoThreaded(GetSongId(), (b) =>
                {
                    if (b)
                    {
                        string songName = new BeatSaver.Song(GetSongId()).SongName;
                        songName = Regex.Replace(songName, "[^a-zA-Z0-9- ]", "");
                        SetSongName(songName);
                    }
                    else SetSongName("[Could not download song info]");
                });
            }
        }
        
        public string GetSongId()
        {
            return songId;
        }

        public string GetSongName()
        {
            return SimpleSql.ExecuteQuery($"SELECT songName FROM songTable WHERE songId = \'{songId}\' AND difficulty = {(int)difficulty}", "songName").First();
        }

        public bool SetSongName(string name)
        {
            return SimpleSql.ExecuteCommand($"UPDATE songTable SET songName = \'{name}\' WHERE songId = \'{songId}\' AND difficulty = {(int)difficulty}") > 1;
        }

        public bool SetOld(bool old)
        {
            return SimpleSql.ExecuteCommand($"UPDATE songTable SET old = \'{(old ? "1" : "0")}\' WHERE songId = \'{songId}\' AND difficulty = {(int)difficulty}") > 1;
        }

        public bool IsOld()
        {
            return SimpleSql.ExecuteQuery($"SELECT old FROM songTable WHERE songId = \'{songId}\' AND difficulty = {(int)difficulty}", "old").First() == "1";
        }

        public int GetGameOptions()
        {
            var optionString = SimpleSql.ExecuteQuery($"SELECT gameOptions FROM songTable WHERE songId = \'{songId}\' AND difficulty = {(int)difficulty}", "gameOptions").First();
            return Convert.ToInt32(optionString);
        }

        public bool SetGameOptions(GameOptions options)
        {
            return SimpleSql.ExecuteCommand($"UPDATE songTable SET gameOptions = {(int)options} WHERE songId = \'{songId}\' AND difficulty = {(int)difficulty}") > 1;
        }

        public int GetPlayerOptions()
        {
            var optionString = SimpleSql.ExecuteQuery($"SELECT playerOptions FROM songTable WHERE songId = \'{songId}\' AND difficulty = {(int)difficulty}", "playerOptions").First();
            return Convert.ToInt32(optionString);
        }

        public bool SetPlayerOptions(PlayerOptions options)
        {
            return SimpleSql.ExecuteCommand($"UPDATE songTable SET playerOptions = {(int)options} WHERE songId = \'{songId}\' AND difficulty = {(int)difficulty}") > 1;
        }

        public bool Exists()
        {
            return Exists(songId, difficulty);
        }

        public static bool Exists(string songId, LevelDifficulty difficulty)
        {
            return SimpleSql.ExecuteQuery($"SELECT * FROM songTable WHERE songId = \'{songId}\' AND difficulty = {(int)difficulty} AND old = 0", "songId").Any();
        }
    }
}
