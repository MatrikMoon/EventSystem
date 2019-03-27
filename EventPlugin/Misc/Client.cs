﻿using TeamSaberPlugin.Helpers;
using TeamSaberPlugin.UI.ViewControllers;
using TeamSaberPlugin.UI.Views;
using TeamSaberShared;
using TeamSaberShared.SimpleJSON;
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
using static TeamSaberShared.SharedConstructs;
using Logger = TeamSaberShared.Logger;
using SongLoaderPlugin.OverrideClasses;

/*
 * Created by Moon on 9/9/2018
 * Communicates with a running DiscordCommunityServer
 */

namespace TeamSaberPlugin.Misc
{
    [Obfuscation(Exclude = false, Feature = "+rename(mode=decodable,renPdb=true)")]
    class Client
    {
        private static string discordCommunityUrl = "http://networkauditor.org";
#if DEBUG
        //private static string discordCommunityApi = $"{discordCommunityUrl}/api-teamsaber-beta";
        private static string discordCommunityApi = $"{discordCommunityUrl}/api-beta";
#else
        //private static string discordCommunityApi = $"{discordCommunityUrl}/api-teamsaber";
        private static string discordCommunityApi = $"{discordCommunityUrl}/api";
#endif
        private static string beatSaverDownloadUrl = "https://beatsaver.com/download/";
        //private static string beatSaverDownloadUrl = "http://bsaber.com/dlsongs/";

        [Obfuscation(Exclude = false, Feature = "-rename;")] //This method is called through reflection, so
        public static void SubmitScore(ulong steamId, string songId, int difficultyLevel, bool fullCombo, int score, string signed, int playerOptions, int gameOptions, Action<bool> scoreUploadedCallback = null)
        {
            //Build score object
            Score s = new Score
            {
                SteamId = steamId.ToString(),
                SongId = songId,
                Score_ = score,
                DifficultyLevel = difficultyLevel,
                FullCombo = fullCombo,
                Signed = signed,
                PlayerOptions = playerOptions,
                GameOptions = gameOptions,
            };

            byte[] scoreData = ProtobufHelper.SerializeProtobuf(s);

            SharedCoroutineStarter.instance.StartCoroutine(SubmitScoreCoroutine(scoreData, scoreUploadedCallback));
        }

        //Post a score to the server
        private static IEnumerator SubmitScoreCoroutine(byte[] proto, Action<bool> scoreUploadedCallback = null)
        {
            JSONObject o = new JSONObject();
            o.Add("pb", new JSONString(Convert.ToBase64String(proto)));

            UnityWebRequest www = UnityWebRequest.Post($"{discordCommunityApi}/submit/", o.ToString());
            www.timeout = 30;
            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
            {
                Logger.Error(www.error);
                scoreUploadedCallback?.Invoke(false);
            }
            else
            {
                Logger.Success("Score upload complete!");
                scoreUploadedCallback?.Invoke(true);
            }
        }

        //Gets the top 10 scores for a song and posts them to the provided leaderboard
        public static void GetSongLeaderboard(CustomLeaderboardController clc, string songId, LevelDifficulty difficulty, Rarity rarity, string teamId, bool useTeamColors = false)
        {
            SharedCoroutineStarter.instance.StartCoroutine(GetSongLeaderboardCoroutine(clc, songId, difficulty, rarity, teamId, useTeamColors));
        }

        //Starts the necessary coroutine chain to make the mod functional
        public static void GetDataForDiscordCommunityPlugin(BeatmapLevelCollectionSO[] lcfgm, SongListViewController slvc, string steamId)
        {
            SharedCoroutineStarter.instance.StartCoroutine(GetAllData(lcfgm, slvc, steamId));
        }

        //Gets all relevant data for the mod to work
        //TODO: If I can parallelize this with song downloading AND get the songs not to try to display when getting
        //profile data fails, that'd be nice.
        private static IEnumerator GetAllData(BeatmapLevelCollectionSO[] lcfgm, SongListViewController slvc, string steamId)
        {
            yield return SharedCoroutineStarter.instance.StartCoroutine(GetUserData(slvc, steamId));
            yield return SharedCoroutineStarter.instance.StartCoroutine(GetTeams(slvc));
            if (!slvc.errorHappened && !slvc.HasSongs()) yield return SharedCoroutineStarter.instance.StartCoroutine(GetSongs(lcfgm, slvc));
        }

        //GET the user's profile data from the server
        private static IEnumerator GetUserData(SongListViewController slvc, string steamId)
        {
            UnityWebRequest www = UnityWebRequest.Get($"{discordCommunityApi}/getplayerstats/{steamId}");
#if DEBUG
            Logger.Info($"GETTING PLAYER DATA: {discordCommunityApi}/getplayerstats/{steamId}");
#endif
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

                    //If there is a message from the server, display it
                    if (node["message"] != null && node["message"].ToString().Length > 1)
                    {
                        slvc.DownloadErrorHappened(node["message"]);
                        yield break;
                    }

                    //If the client is out of date, show update message
                    if (VersionCode < Convert.ToInt32(node["version"].Value))
                    {
                        slvc.DownloadErrorHappened($"Version {SharedConstructs.Version} is now out of date. Please download the newest one from the Discord.");
                    }

                    Player.Instance.rarity = (Rarity)Convert.ToInt64(node["rarity"].Value);
                    Player.Instance.team = node["team"].ToString();
                }
                catch (Exception e)
                {
                    Logger.Error($"Error parsing getplayerstats data: {e}");
                    slvc.DownloadErrorHappened($"Error parsing getplayerstats data: {e}");
                }
            }
        }

        private static IEnumerator GetSongLeaderboardCoroutine(CustomLeaderboardController clc, string songId, LevelDifficulty difficulty, Rarity rarity, string teamId = "-1", bool useTeamColors = false)
        {
            UnityWebRequest www = UnityWebRequest.Get($"{discordCommunityApi}/getsongleaderboards/{songId}/{(int)difficulty}/{(int)rarity}/{teamId}");
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
                    List<CustomLeaderboardTableView.CustomScoreData> scores = new List<CustomLeaderboardTableView.CustomScoreData>();
                    int myPos = -1;
                    foreach (var score in node)
                    {
                        scores.Add(new CustomLeaderboardTableView.CustomScoreData(
                            Convert.ToInt32(score.Value["score"].ToString()),
                            score.Value["player"],
                            Convert.ToInt32(score.Value["place"].ToString()),
                            score.Value["fullCombo"] == "true",
                            (Rarity)Convert.ToInt32(score.Value["rarity"].ToString()),
                            score.Value["team"]
                        ));

                        //If one of the scores is us, set the "special" score position to the right value
                        if (score.Value["steamId"] == Convert.ToString(Plugin.PlayerId))
                        {
                            myPos = Convert.ToInt32(score.Value["place"] - 1);
                        }
                    }
                    clc.SetScores(scores, myPos, useTeamColors);
                }
                catch (Exception e)
                {
                    Logger.Error($"Error parsing leaderboard data: {e}");
                }
            }
        }

        //GET the weekly songs from the server, then start the Download coroutine to download and display them
        //TODO: Time complexity here is a mess.
        private static IEnumerator GetTeams(SongListViewController slvc)
        {
            UnityWebRequest www = UnityWebRequest.Get($"{discordCommunityApi}/getteams/");
#if DEBUG
            Logger.Info($"REQUESTING TEAMS: {discordCommunityApi}/getteams/");
#endif
            www.timeout = 30;
            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
            {
                Logger.Error($"Error getting teams: {www.error}");
                slvc.DownloadErrorHappened($"Error getting teams: {www.error}");
            }
            else
            {
                try
                {
                    //Clear out existing teams
                    Team.allTeams.Clear();

                    //Get the list of songs to download, and map out the song ids to the corresponding gamemodes
                    var node = JSON.Parse(www.downloadHandler.text);
                    foreach (var team in node)
                    {
                        Team.allTeams.Add(new Team(team.Key, team.Value["teamName"], team.Value["captainId"], team.Value["color"]));
                    }
                }
                catch (Exception e)
                {
                    Logger.Error($"Error parsing getteams data: {e}");
                    slvc.DownloadErrorHappened($"Error parsing getteams data: {e}");
                    yield break;
                }
            }
        }

        //GET the weekly songs from the server, then start the Download coroutine to download and display them
        //TODO: Time complexity here is a mess.
        private static IEnumerator GetSongs(BeatmapLevelCollectionSO[] lcfgm, SongListViewController slvc)
        {
            UnityWebRequest www = UnityWebRequest.Get($"{discordCommunityApi}/getweeklysongs/");
#if DEBUG
            Logger.Info($"REQUESTING WEEKLY SONGS: {discordCommunityApi}/getweeklysongs/");
#endif
            www.timeout = 30;
            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
            {
                Logger.Error($"Error getting weekly songs: {www.error}");
                slvc.DownloadErrorHappened($"Error getting weekly songs: {www.error}");
            }
            else
            {
                List<Song> songs = new List<Song>();
                try
                {
                    //Get the list of songs to download, and map out the song ids to the corresponding gamemodes
                    var node = JSON.Parse(www.downloadHandler.text);
                    foreach (var id in node)
                    {
                        var newSong = new Song()
                        {
                            SongId = id.Value["songId"],
                            SongName = id.Value["songName"],
                            GameOptions = (GameOptions)Convert.ToInt32(id.Value["gameOptions"].ToString()),
                            PlayerOptions = (PlayerOptions)Convert.ToInt32(id.Value["playerOptions"].ToString()),
                            Difficulty = (LevelDifficulty)Convert.ToInt32(id.Value["difficulty"].ToString())
                        };

                        if (newSong.Difficulty == LevelDifficulty.Auto) newSong.Difficulty = Player.Instance.GetPreferredDifficulty(OstHelper.IsOst(newSong.SongId));

                        songs.Add(newSong);
                    }
                }
                catch (Exception e)
                {
                    Logger.Error($"Error parsing getweeklysong data: {e}");
                    slvc.DownloadErrorHappened($"Error parsing getweeklysong data: {e}");
                    yield break;
                }

                //If we got songs, filter them as neccessary then download any we don't have
                List<Song> availableSongs = new List<Song>();

                //Filter out songs we already have and OSTS
                IEnumerable<Song> osts = songs.Where(x => OstHelper.IsOst(x.SongId));
                IEnumerable<Song> alreadyHave = songs.Where(x => SongIdHelper.GetSongExistsBySongId(x.SongId));

                //Loads a level from a song instance, populates the Beatmap property and adds to the available list
                Action<Song> loadLevel = (song) =>
                {
                    var level = SongIdHelper.GetLevelFromSongId(song.SongId);

                    //If the directory exists, but isn't loaded in song loader,
                    //there's probably a conflict with another loaded song
                    if (level == null)
                    {
                        slvc.DownloadErrorHappened($"Could not load level {song.SongName}. You probably have an older version ('{song.SongId.Substring(0, song.SongId.IndexOf("-"))}') already downloded. Please remove this or save it elsewhere to continue.");
                    }

                    //TODO: add characteristic name field to the song data stored in the server
                    song.Beatmap = Player.Instance.GetClosestDifficultyPreferLower(level as BeatmapLevelSO, (BeatmapDifficulty)song.Difficulty);
                    availableSongs.Add(song);
                };

                //Of what we already have, add the Levels to the availableSongs list
                alreadyHave.ToList().ForEach(x => loadLevel(x));

                //If there's an error at this point, one of the levels failed to load. Do not continue.
                if (slvc.errorHappened) yield break;

                osts.ToList().ForEach(x =>
                {
                    //TODO: Time complexity fix?
                    var level = lcfgm
                                    .FirstOrDefault(y => y.beatmapLevels.Any(z => z.levelID == x.SongId)).beatmapLevels
                                    .FirstOrDefault(y => y.levelID == x.SongId) as BeatmapLevelSO;
                    x.Beatmap = Player.Instance.GetClosestDifficultyPreferLower(level, (BeatmapDifficulty)x.Difficulty);
                    availableSongs.Add(x);
                });

                //Remove what we already have
                songs.RemoveAll(x => alreadyHave.Select(y => y.SongId).Contains(x.SongId) || osts.Contains(x)); //Don't redownload

                //Download the things we don't have, or if we have everything, show the menu
                if (songs.Count > 0)
                {
                    List<IEnumerator> downloadCoroutines = new List<IEnumerator>();
                    songs.ForEach(x =>
                    {
                        downloadCoroutines.Add(DownloadSongs(x.SongId, slvc));
                    });

                    //Wait for the all downloads to finish
                    yield return SharedCoroutineStarter.instance.StartCoroutine(new ParallelCoroutine().ExecuteCoroutines(downloadCoroutines.ToArray()));

                    Action<SongLoader, List<CustomLevel>> songsLoaded =
                        (SongLoader sender, List<CustomLevel> loadedSongs) =>
                        {
                            //Now that they're refreshed, we can populate their beatmaps and add them to the available list
                            songs.ForEach(x => loadLevel(x));
                            availableSongs.AddRange(songs);
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
        //availableSongs: List of IBeatmapLevel which may hold levels already approved for display
        //downloadQueue: List of beatsaver ids representing songs left to download
        //completedDownloads: List of beatsaver ids representing songs that have successfully downloaded
        //songId: The song this instance of the Coroutine is supposed to download
        //slvc: The song list view controller to display the downloaded songs to
        private static IEnumerator DownloadSongs(string songId, SongListViewController slvc)
        {
            UnityWebRequest www = UnityWebRequest.Get($"{beatSaverDownloadUrl}{songId}");
#if DEBUG
            Logger.Info($"DOWNLOADING: {beatSaverDownloadUrl}{songId}");
#endif
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
                Logger.Error($"Error downloading song {songId}: {www.error}");
                slvc.DownloadErrorHappened($"Error downloading song {songId}: {www.error}");
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