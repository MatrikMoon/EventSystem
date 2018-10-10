using DiscordCommunityShared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/*
 * Created by Moon on 9/11/2018
 * TODO: Use Properties (get/set) instead of getters and setters
 */

namespace DiscordCommunityServer.Database
{
    class Song
    {
        private string songId;
        private SharedConstructs.GameplayMode mode;

        public Song(string songId, int mode)
        {
            this.songId = songId;
            this.mode = (SharedConstructs.GameplayMode)mode;
            if (!Exists())
            {
                //Add a placeholder, trigger song download from BeatSaver if it doesn't exist
                SimpleSql.AddSong("", "", "", songId, mode);
                if (OstHelper.IsOst(songId))
                {
                    SetSongName(OstHelper.GetOstSongNameFromLevelId(songId));
                }
                else BeatSaver.BeatSaverDownloader.UpdateSongInfoThreaded(this);
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

        public SharedConstructs.GameplayMode GetSongMode()
        {
            return (SharedConstructs.GameplayMode)Convert.ToInt32(SimpleSql.ExecuteQuery($"SELECT mode FROM songTable WHERE songId = \'{songId}\'", "mode").First());
        }

        public bool SetSongMode(int mode)
        {
            return SimpleSql.ExecuteCommand($"UPDATE songTable SET mode = {mode} WHERE songId = \'{songId}\'") > 1;
        }

        public bool Exists()
        {
            return Exists(songId, (int)mode);
        }

        public static bool Exists(string songId, int mode)
        {
            return SimpleSql.ExecuteQuery($"SELECT * FROM songTable WHERE songId = \'{songId}\' AND mode = {mode}", "songId").Any();
        }
    }
}
