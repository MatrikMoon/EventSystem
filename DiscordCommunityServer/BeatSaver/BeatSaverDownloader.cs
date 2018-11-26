using DiscordCommunityShared;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Threading;

/*
 * Created by Moon on 9/11/2018 (Inspired by andruzzzhka's BeatSaverMultiplayer)
 * Handles the downloading and unzipping of songs from Beatsaver
 */

namespace DiscordCommunityServer.BeatSaver
{
    class BeatSaverDownloader
    {
        private static string beatSaverUrl = "https://beatsaver.com";
        //private static string beatSaverUrl = "http://bsaber.com";
        private static string beatSaverDownloadUrl = $"{beatSaverUrl}/download/";
        //private static string beatSaverDownloadUrl = $"{beatSaverUrl}/dlsongs/";

        public static string DownloadSong(string id)
        {
            Logger.Info($"Downloading {id} from {beatSaverUrl}");

            //Create DownloadedSongs if it doesn't exist
            Directory.CreateDirectory(Song.songDirectory);

            //Don't download if we already have it
            if (Directory.GetDirectories(Song.songDirectory).All(o => o != $"{Song.songDirectory}{id}"))
            {
                string zipPath = $"{Song.songDirectory}{id}.zip";

                //Download zip
                using (var client = new WebClient())
                {
                    client.Headers.Add("user-agent", "DiscordCommunityServer");

                    try
                    {
                        client.DownloadFile($"{beatSaverDownloadUrl}{id}", zipPath);
                    }
                    catch (Exception e)
                    {
                        Logger.Error($"Error downloading {id}.zip: {e}");
                        return null;
                    }
                }

                //Unzip to folder
                using (ZipArchive zip = ZipFile.OpenRead(zipPath))
                {
                    zip?.ExtractToDirectory($@"{Song.songDirectory}{id}\");
                }

                //Clean up zip
                File.Delete(zipPath);
            }

            Logger.Success($"Downloaded {Directory.GetDirectories($"{Song.songDirectory}{id}").First()}!");

            return $@"{Song.songDirectory}{id}\";
        }

        public static void UpdateSongInfoThreaded(Database.Song song)
        {
            new Thread(() =>
            {
                string songDir = DownloadSong(song.GetSongId());
                if (songDir != null)
                {
                    string songName = new Song(song.GetSongId()).SongName;
                    song.SetSongName(songName);
                }
            })
            .Start();
        }
    }
}
