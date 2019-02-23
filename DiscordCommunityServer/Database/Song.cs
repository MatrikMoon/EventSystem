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
                SimpleSql.AddSong("", "", "", songId, difficulty);
                SetOld(true); //Start off by setting the song to old, just in case we don't want to add it to the plugin list
                if (OstHelper.IsOst(songId))
                {
                    SetSongName(OstHelper.GetOstSongNameFromLevelId(songId));
                }
                else BeatSaver.BeatSaverDownloader.DownloadSongInfoThreaded(GetSongId(), (b) =>
                {
                    if (b)
                    {
                        string songName = new BeatSaver.Song(GetSongId()).SongName;
                        songName = Regex.Replace(songName, "[^a-zA-Z0-9-]", "");
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
            return SimpleSql.ExecuteQuery($"SELECT songName FROM songTable WHERE songId = \'{songId}\'", "songName").First();
        }

        public bool SetSongName(string name)
        {
            return SimpleSql.ExecuteCommand($"UPDATE songTable SET songName = \'{name}\' WHERE songId = \'{songId}\'") > 1;
        }

        public bool SetOld(bool old)
        {
            return SimpleSql.ExecuteCommand($"UPDATE songTable SET old = \'{(old ? "1" : "0")}\' WHERE songId = \'{songId}\'") > 1;
        }

        public bool IsOld()
        {
            return SimpleSql.ExecuteQuery($"SELECT old FROM songTable WHERE songId = \'{songId}\'", "old").First() == "1";
        }

        public bool Exists()
        {
            return Exists(songId, difficulty);
        }

        public static bool Exists(string songId, LevelDifficulty difficulty)
        {
            return SimpleSql.ExecuteQuery($"SELECT * FROM songTable WHERE songId = \'{songId}\' AND difficulty = {(int)difficulty} AND OLD = 0", "songId").Any();
        }
    }
}
