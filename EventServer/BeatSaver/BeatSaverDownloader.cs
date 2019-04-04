using EventShared;
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

namespace EventServer.BeatSaver
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
                    client.Headers.Add("user-agent", "EventServer");

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
            else Logger.Success("Song already downloaded! Skipping download!");

            var idFolder = $"{Song.songDirectory}{id}";
            var songFolder = Directory.GetDirectories(idFolder); //Assuming each id folder has only one song folder
            var subFolder = songFolder.FirstOrDefault() ?? idFolder;
            Logger.Success($"Downloaded {subFolder}!");

            return $@"{Song.songDirectory}{id}\";
        }

        public static void DownloadSongInfoThreaded(string songId, Action<bool> whenFinished)
        {
            new Thread(() =>
            {
                string songDir = DownloadSong(songId);
                whenFinished?.Invoke(songDir != null);
            })
            .Start();
        }
    }
}
