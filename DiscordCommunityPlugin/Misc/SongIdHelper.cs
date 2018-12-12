using DiscordCommunityShared;
using SongLoaderPlugin;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

/*
 * Created by Moon on 9/12/2018
 * There's no easy way to go from a `Level` object to a song id in Beat Saber.
 * This class leverages Paths to help figure out a song's ID.
 * This is hacky, but it works.
 */

namespace ChristmasVotePlugin.Misc
{
    [Obfuscation(Exclude = false, Feature = "+rename(mode=decodable,renPdb=true)")]
    class SongIdHelper
    {
        public static string GetSongIdFromLevelId(string levelId)
        {
            //Hacky way of getting the song id, through getting the file path from SongLoader
            string songPath = SongLoader.CustomLevels.Find(x => x.levelID == levelId).customSongInfo.path;

            //Yet another hacky fix for when songs are improperly uploaded, with no internal directory, only ID > files
            var name = Directory.GetParent(songPath).Name;
            if (name == "CustomSongs") name = new DirectoryInfo(songPath).Name;
            return name;
        }
        
        //Assuming the id exists, returns the IBeatmapLevel of the level corresponding to the id
        public static IBeatmapLevel GetLevelFromSongId(string songId)
        {
            return SongLoader.CustomLevels.Find(x => songId == Directory.GetParent(x.customSongInfo.path).Name || songId == new DirectoryInfo(x.customSongInfo.path).Name); //Note: VERY RARELY a song will not have an internal directory
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
