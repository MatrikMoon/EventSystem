using DiscordCommunityShared;
using DiscordCommunityShared.SimpleJSON;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static DiscordCommunityShared.SharedConstructs;

/*
 * Created by Moon on 9/25/2018
 * This class is intended to handle the reading of data from
 * songs downloaded from BeatSaver
 */

namespace DiscordCommunityServer.BeatSaver
{
    class Song
    {
        public static readonly string currentDirectory = Directory.GetCurrentDirectory();
        public static readonly string songDirectory = $@"{currentDirectory}\DownloadedSongs\";

        public string SongName { get; }

        string SongId { get; set; }

        private string _infoPath;

        public Song(string songId)
        {
            SongId = songId;

            _infoPath = GetInfoPath();
            SongName = GetSongName();
        }

        //Looks at info.json and gets the song name
        private string GetSongName()
        {
            var infoText = File.ReadAllText(_infoPath);
            JSONNode node = JSON.Parse(infoText);
            return node["songName"];
        }

        private string GetInfoPath()
        {
            var songFolder = Directory.GetDirectories($"{songDirectory}{SongId}").First(); //Assuming each id folder has only one song folder
            return Directory.GetFiles(songFolder, "info.json", SearchOption.AllDirectories).First(); //Assuming each song folder has only one info.json
        }
    }
}
