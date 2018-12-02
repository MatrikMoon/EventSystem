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

        public Song(string songId)
        {
            this.songId = songId;
            if (!Exists())
            {
                //Add a placeholder, trigger song download from BeatSaver if it doesn't exist
                SimpleSql.AddSong("", "", "", songId);
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

        public bool IsOld()
        {
            return SimpleSql.ExecuteQuery($"SELECT old FROM songTable WHERE songId = \'{songId}\'", "old").First() == "1";
        }

        public bool Exists()
        {
            return Exists(songId);
        }

        public static bool Exists(string songId)
        {
            return SimpleSql.ExecuteQuery($"SELECT * FROM songTable WHERE songId = \'{songId}\'", "songId").Any();
        }
    }
}
