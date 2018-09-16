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

namespace DiscordCommunityServer.Misc
{
    class BeatSaverDownloader
    {
        private static string beatSaverUrl = "https://beatsaver.com";
        private static string currentDirectory = Directory.GetCurrentDirectory();
        private static string songDirectory = $@"{currentDirectory}\DownloadedSongs\";

        public static string DownloadSong(string id)
        {
            Logger.Info($"Downloading {id} from {beatSaverUrl}");

            //Create DownloadedSongs if it doesn't exist
            Directory.CreateDirectory(songDirectory);

            //Don't download if we already have it
            if (Directory.GetDirectories(songDirectory).All(o => o != $"{songDirectory}{id}"))
            {
                string zipPath = $"{songDirectory}{id}.zip";

                //Download zip
                using (var client = new WebClient())
                {
                    client.Headers.Add("user-agent", "DiscordCommunityServer");

                    try
                    {
                        client.DownloadFile($"{beatSaverUrl}/download/{id}", zipPath);
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
                    zip?.ExtractToDirectory($@"{songDirectory}{id}\");
                }

                //Clean up zip
                File.Delete(zipPath);
            }

            Logger.Success($"Downloaded {Directory.GetDirectories($"{songDirectory}{id}").First()}!");

            return $@"{songDirectory}{id}\";
        }

        //TODO: Proper song info-getting from json
        public static void UpdateSongInfoThreaded(Database.Song song)
        {
            new Thread(() =>
            {
                string songDir = DownloadSong(song.GetSongId());
                if (songDir != null)
                {
                    string songName = Path.GetFileName(Directory.GetDirectories(songDir).First());
                    song.SetSongName(songName);
                }
            })
            .Start();
        }
    }
}
