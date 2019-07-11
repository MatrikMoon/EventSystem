using EventShared;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

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
        private static string beatSaverSongPageUrl = $"{beatSaverUrl}/beatmap/";
        private static string beatSaverDownloadByHashUrl = $"{beatSaverUrl}/api/download/hash/";
        private static string beatSaverDownloadByKeyUrl = $"{beatSaverUrl}/api/download/key/";
        //private static string beatSaverDownloadUrl = $"{beatSaverUrl}/dlsongs/";

        public static string DownloadSong(string hash)
        {
            Logger.Info($"Downloading {hash} from {beatSaverUrl}");

            //Create DownloadedSongs if it doesn't exist
            Directory.CreateDirectory(Song.songDirectory);

            //Get the hash of the indicated song
            string zipPath = $"{Song.songDirectory}{hash}.zip";

            //Download zip
            using (var client = new WebClient())
            {
                client.Headers.Add("user-agent", "EventServer");

                try
                {
                    //Don't download if we already have it
                    if (Directory.GetDirectories(Song.songDirectory).All(o => o != $"{Song.songDirectory}{hash}"))
                    {
                        client.DownloadFile($"{beatSaverDownloadByHashUrl}{hash}", zipPath);

                        //Unzip to folder
                        using (ZipArchive zip = ZipFile.OpenRead(zipPath))
                        {
                            zip?.ExtractToDirectory($@"{Song.songDirectory}{hash}\");
                        }

                        //Clean up zip
                        File.Delete(zipPath);
                    }
                    else Logger.Success("Song already downloaded! Skipping download!");
                }
                catch (Exception e)
                {
                    Logger.Error($"Error downloading {hash}.zip: {e}");
                    return null;
                }
            }

            var idFolder = $"{Song.songDirectory}{hash}";
            var songFolder = Directory.GetDirectories(idFolder); //Assuming each id folder has only one song folder
            var subFolder = songFolder.FirstOrDefault() ?? idFolder;
            Logger.Success($"Downloaded {subFolder}!");

            return $@"{Song.songDirectory}{hash}\";
        }

        public static void DownloadSongInfoThreaded(string hash, Action<bool> whenFinished)
        {
            new Thread(() =>
            {
                if (!Song.Exists(hash))
                {
                    string songDir = DownloadSong(hash);
                    whenFinished?.Invoke(songDir != null);
                }
                else whenFinished?.Invoke(true);
            })
            .Start();
        }

        public static string GetHashFromID(string id)
        {
            if (OstHelper.IsOst(id)) return id;

            Logger.Info($"Getting hash for {id} from {beatSaverUrl}");


            HttpClientHandler httpClientHandler = new HttpClientHandler();
            httpClientHandler.AllowAutoRedirect = false;

            using (var client = new HttpClient(httpClientHandler))
            {
                client.DefaultRequestHeaders.Add("user-agent", "EventServer");

                var response = client.GetAsync($"{beatSaverDownloadByKeyUrl}{id}");
                response.Wait();

                var result = response.Result.Headers.Location.ToString();
                var startIndex = result.LastIndexOf("/") + 1;
                var length = result.LastIndexOf(".") - startIndex;

                return result.Substring(startIndex, length);
            }
        }
    }
}
