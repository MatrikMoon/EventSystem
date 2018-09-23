using SongLoaderPlugin;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

/*
 * Created by Moon on 9/12/2018
 * There's no easy way to go from a `Level` object to a song id in Beat Saber.
 * This class leverages Paths to help figure out a song's ID.
 * This is hacky, but it works.
 */

namespace DiscordCommunityPlugin.Misc
{
    [Obfuscation(Exclude = false, Feature = "+rename(mode=decodable,renPdb=true)")]
    class SongIdHelper
    {
        public static string GetSongIdFromLevelId(string levelId)
        {
            if (levelId.StartsWith("Level")) return levelId;

            //Hacky way of getting the song id, through getting the file path from SongLoader
            string songPath = SongLoader.CustomLevels.Find(x => x.levelID == levelId).customSongInfo.path;
            return Directory.GetParent(songPath).Name;
        }
        
        //Assuming the id exists, returns the IStandardLevel of the level corresponding to the id
        public static IStandardLevel GetLevelFromSongId(string songId)
        {
            return SongLoader.CustomLevels.Find(x => songId == Directory.GetParent(x.customSongInfo.path).Name);
        }

        public static bool GetSongExistsBySongId(string songId)
        {
            //Checks directory names for the song id
            var path = Environment.CurrentDirectory;
            var songFolders = Directory.GetDirectories(path + "\\CustomSongs").ToList();
            return songFolders.Any(x => Path.GetFileName(x) == songId);
        }

    }
}
