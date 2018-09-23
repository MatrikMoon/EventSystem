using DiscordCommunityPlugin.UI.ViewControllers;
using DiscordCommunityShared;
using DiscordCommunityShared.SimpleJSON;
using SongLoaderPlugin;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;
using static DiscordCommunityShared.SharedConstructs;
using Logger = DiscordCommunityShared.Logger;

/*
 * Created by Moon on 9/9/2018
 * Communicates with a running DiscordCommunityServer
 */

namespace DiscordCommunityPlugin.Misc
{
    [Obfuscation(Exclude = false, Feature = "+rename(mode=decodable,renPdb=true)")]
    class Client
    {
        private static string discordCommunityUrl = "https://networkauditor.org";
#if DEBUG
        private static string discordCommunityApi = $"{discordCommunityUrl}/api-beta";
#else
        private static string discordCommunityApi = $"{discordCommunityUrl}/api";
#endif
        private static string beatSaverDownloadUrl = "https://beatsaver.com/download/";

        [Obfuscation(Exclude = false, Feature = "-rename;")] //This method is called through reflection, so
        public static void SubmitScore(ulong steamId, string songId, int difficultyLevel, int gameplayMode, bool fullCombo, int score, string signed)
        {
            //Build score object
            Score s = new Score
            {
                SteamId = steamId.ToString(),
                SongId = songId,
                Score_ = score,
                DifficultyLevel = difficultyLevel,
                GameplayMode = gameplayMode,
                FullCombo = fullCombo,
                Signed = signed
            };

            byte[] scoreData = ProtobufHelper.SerializeProtobuf(s);

            SharedCoroutineStarter.instance.StartCoroutine(SubmitScoreCoroutine(scoreData));
        }

        //Post a score to the server
        private static IEnumerator SubmitScoreCoroutine(byte[] proto)
        {
            JSONObject o = new JSONObject();
            o.Add("pb", new JSONString(Convert.ToBase64String(proto)));

            UnityWebRequest www = UnityWebRequest.Post($"{discordCommunityApi}/submit/", o.ToString());
            www.timeout = 30;
            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
            {
                Logger.Error(www.error);
            }
            else
            {
                Logger.Success("Score upload complete!");
            }
        }

        //Gets the top 10 scores for a song and posts them to the provided leaderboard
        public static void GetSongLeaderboard(CustomLeaderboardController clc, string songId, Rank rank)
        {
            SharedCoroutineStarter.instance.StartCoroutine(GetSongLeaderboardCoroutine(clc, songId, rank));
        }

        //Starts the necessary coroutine chain to make the mod functional
        public static void GetDataForDiscordCommunityPlugin(LevelCollectionsForGameplayModes lcfgm, SongListViewController slvc, string steamId)
        {
            SharedCoroutineStarter.instance.StartCoroutine(GetAllData(lcfgm, slvc, steamId));
        }

        //Gets all relevant data for the mod to work
        //TODO: If I can parallelize this with song downloading AND get the songs not to try to display when getting
        //profile data fails, that'd be nice.
        private static IEnumerator GetAllData(LevelCollectionsForGameplayModes lcfgm, SongListViewController slvc, string steamId)
        {
            yield return SharedCoroutineStarter.instance.StartCoroutine(GetUserData(slvc, steamId));
            if (!slvc.errorHappened && !slvc.HasSongs()) yield return SharedCoroutineStarter.instance.StartCoroutine(GetWeeklySongs(lcfgm, slvc));
        }

        //GET the user's profile data from the server
        private static IEnumerator GetUserData(SongListViewController slvc, string steamId)
        {
            UnityWebRequest www = UnityWebRequest.Get($"{discordCommunityApi}/getplayerstats/{steamId}");
            www.timeout = 30;
            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
            {
                Logger.Error($"Error getting player stats: {www.error}");
                slvc.DownloadErrorHappened($"Error getting player stats: {www.error}");
            }
            else
            {
                try
                {
                    var node = JSON.Parse(www.downloadHandler.text);

                    //If the client is out of date, show update message
                    if (VersionCode < Convert.ToInt32(node["version"].Value))
                    {
                        slvc.DownloadErrorHappened($"Version {SharedConstructs.Version} is now out of date. Please download the newest one from the Discord.");
                    }

                    DiscordCommunityHelpers.Player.Instance.rank = (Rank)Convert.ToInt64(node["rank"].Value);
                    DiscordCommunityHelpers.Player.Instance.tokens = Convert.ToInt64(node["tokens"].Value);
                }
                catch (Exception e)
                {
                    Logger.Error($"Error parsing getplayerstats data: {e}");
                    slvc.DownloadErrorHappened($"Error parsing getplayerstats data: {e}");
                }
            }
        }

        private static IEnumerator GetSongLeaderboardCoroutine(CustomLeaderboardController clc, string songId, Rank rank)
        {
            UnityWebRequest www = UnityWebRequest.Get($"{discordCommunityApi}/getsongleaderboards/{songId}/{(int)rank}/{(int)DiscordCommunityHelpers.Player.Instance.desiredModes[songId]}");
            www.timeout = 30;
            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
            {
                Logger.Error($"Error getting leaderboard data: {www.error}");
            }
            else
            {
                try
                {
                    var node = JSON.Parse(www.downloadHandler.text);
                    List<LeaderboardTableView.ScoreData> scores = new List<LeaderboardTableView.ScoreData>();
                    int myPos = -1;
                    foreach (var score in node)
                    {
                        scores.Add(new LeaderboardTableView.ScoreData(
                            Convert.ToInt32(score.Value["score"].ToString()),
                            score.Value["player"],
                            Convert.ToInt32(score.Value["rank"].ToString()),
                            score.Value["fullCombo"] == "true"));

                        //If one of the scores is us, set the "special" score position to the right value
                        if (score.Value["steamId"] == Convert.ToString(Plugin.PlayerId))
                        {
                            myPos = Convert.ToInt32(score.Value["rank"] - 1);
                        }
                    }
                    clc.SetScores(scores, myPos);
                }
                catch (Exception e)
                {
                    Logger.Error($"Error parsing leaderboard data: {e}");
                }
            }
        }

        //GET the weekly songs from the server, then start the Download coroutine to download and display them
        //TODO: Time complexity here is a mess.
        private static IEnumerator GetWeeklySongs(LevelCollectionsForGameplayModes lcfgm, SongListViewController slvc)
        {
            UnityWebRequest www = UnityWebRequest.Get($"{discordCommunityApi}/getweeklysongs/");
            www.timeout = 30;
            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
            {
                Logger.Error($"Error getting weekly songs: {www.error}");
                slvc.DownloadErrorHappened($"Error getting weekly songs: {www.error}");
            }
            else
            {
                List<string> songIds = new List<string>();
                try
                {
                    //Get the list of songs to download, and map out the song ids to the corresponding gamemodes
                    var node = JSON.Parse(www.downloadHandler.text);
                    DiscordCommunityHelpers.Player.Instance.desiredModes = new Dictionary<string, GameplayMode>();
                    foreach (var id in node)
                    {
                        songIds.Add(id.Key);
                        DiscordCommunityHelpers.Player.Instance.desiredModes.Add(id.Key, (GameplayMode)Convert.ToInt32(id.Value.Value));
                    }
                }
                catch (Exception e)
                {
                    Logger.Error($"Error parsing getweeklysong data: {e}");
                    slvc.DownloadErrorHappened($"Error parsing getweeklysong data: {e}");
                    yield break;
                }

                //If we got songs, filter them as neccessary then download any we don't have
                List<IStandardLevel> availableSongs = new List<IStandardLevel>();

                //Filter out songs we already have and OSTS
                IEnumerable<string> osts = songIds.Where(x => x.StartsWith("Level"));
                IEnumerable<string> alreadyHave = songIds.Where(x => SongIdHelper.GetSongExistsBySongId(x));

                //Of what we already have, add the Levels to the availableSongs list
                alreadyHave.ToList().ForEach(x => {
                    var level = SongIdHelper.GetLevelFromSongId(x);

                    //If the directory exists, but isn't loaded in song loader,
                    //there's probably a conflict with another loaded song
                    if (level == null)
                    {
                        slvc.DownloadErrorHappened($"Could not load level {x}. You probably have an older version ('{x.Substring(0, x.IndexOf("-"))}') already downloded. Please remove this or save it elsewhere to continue.");
                    }
                    availableSongs.Add(level);
                });

                //If there's an error at this point, one of the levels failed to load. Do not continue.
                if (slvc.errorHappened) yield break;

                osts.ToList().ForEach(x => availableSongs.Add(lcfgm.GetLevels(DiscordCommunityHelpers.Player.Instance.desiredModes[x]).Where(y => y.levelID == x).First()));

                //Remove the id's of what we already have
                songIds.RemoveAll(x => alreadyHave.Contains(x) || osts.Contains(x)); //Don't redownload

                //Download the things we don't have, or if we have everything, show the menu
                if (songIds.Count > 0)
                {
                    List<IEnumerator> downloadCoroutines = new List<IEnumerator>();
                    songIds.ForEach(x =>
                    {
                        downloadCoroutines.Add(DownloadWeeklySongs(x, slvc));
                    });

                    //Wait for the all downloads to finish
                    yield return SharedCoroutineStarter.instance.StartCoroutine(new ParallelCoroutine().ExecuteCoroutines(downloadCoroutines.ToArray()));

                    Action<SongLoader, List<SongLoaderPlugin.OverrideClasses.CustomLevel>> songsLoaded =
                        (SongLoader sender, List<SongLoaderPlugin.OverrideClasses.CustomLevel> loadedSongs) =>
                        {
                            //Now that they're refreshed, we can add them to the available list
                            songIds.ForEach(x => availableSongs.Add(SongIdHelper.GetLevelFromSongId(x)));

                            slvc.SetSongs(availableSongs);
                        };

                    SongLoader.SongsLoadedEvent -= songsLoaded;
                    SongLoader.SongsLoadedEvent += songsLoaded;
                    SongLoader.Instance.RefreshSongs(false);
                }
                else
                {
                    slvc.SetSongs(availableSongs);
                }
            }
        }

        //Download songs. Taken from BeatSaberMultiplayer
        //availableSongs: List of IStandardLevel which may hold levels already approved for display
        //downloadQueue: List of beatsaver ids representing songs left to download
        //completedDownloads: List of beatsaver ids representing songs that have successfully downloaded
        //songId: The song this instance of the Coroutine is supposed to download
        //slvc: The song list view controller to display the downloaded songs to
        private static IEnumerator DownloadWeeklySongs(string songId, SongListViewController slvc)
        {
            UnityWebRequest www = UnityWebRequest.Get($"{beatSaverDownloadUrl}{songId}");

            bool timeout = false;
            float time = 0f;

            UnityWebRequestAsyncOperation asyncRequest = www.SendWebRequest();

            while (!asyncRequest.isDone || asyncRequest.progress < 1f)
            {
                yield return null;

                time += Time.deltaTime;

                if (time >= 15f && asyncRequest.progress == 0f)
                {
                    www.Abort();
                    timeout = true;
                }
            }

            if (www.isNetworkError || www.isHttpError || timeout)
            {
                Logger.Error($"Downloading error: {www.error}");
                slvc.DownloadErrorHappened($"Downloading error: {www.error}");
            }
            else
            {
                //Logger.Info("Received response from BeatSaver.com...");

                string zipPath = "";
                string docPath = "";
                string customSongsPath = "";

                byte[] data = www.downloadHandler.data;

                try
                {
                    docPath = Application.dataPath;
                    docPath = docPath.Substring(0, docPath.Length - 5);
                    docPath = docPath.Substring(0, docPath.LastIndexOf("/"));
                    customSongsPath = docPath + "/CustomSongs/" + songId + "/";
                    zipPath = customSongsPath + songId + ".zip";
                    if (!Directory.Exists(customSongsPath))
                    {
                        Directory.CreateDirectory(customSongsPath);
                    }
                    File.WriteAllBytes(zipPath, data);
                    //Logger.Info("Downloaded zip file!");
                }
                catch (Exception e)
                {
                    Logger.Error($"Error writing zip: {e}");
                    slvc.DownloadErrorHappened($"Error writing zip: {e}");
                    yield break;
                }

                //Logger.Info("Extracting...");

                try
                {
                    ZipFile.ExtractToDirectory(zipPath, customSongsPath);
                }
                catch (Exception e)
                {
                    Logger.Error($"Unable to extract ZIP! Exception: {e}");
                    slvc.DownloadErrorHappened($"Unable to extract ZIP! Exception: {e}");
                    yield break;
                }

                try
                {
                    File.Delete(zipPath);
                }
                catch (IOException e)
                {
                    Logger.Warning($"Unable to delete zip! Exception: {e}");
                    slvc.DownloadErrorHappened($"Unable to delete zip! Exception: {e}");
                    yield break;
                }

                Logger.Success($"Downloaded!");
            }
        }
    }
}
