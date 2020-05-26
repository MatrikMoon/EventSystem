using EventPlugin.Models;
using EventPlugin.UI.ViewControllers;
using EventPlugin.UI.Views;
using EventShared;
using EventShared.SimpleJSON;
using SongCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;
using static EventShared.SharedConstructs;
using Logger = EventShared.Logger;

/*
 * Created by Moon on 9/9/2018
 * Communicates with a running EventServer
 */

namespace EventPlugin.Misc
{
    [Obfuscation(Exclude = false, Feature = "+rename(mode=decodable,renPdb=true)")]
    class Client
    {
        private static string discordCommunityUrl = "http://networkauditor.org";
#if (TEAMSABER)
        private static string discordCommunityApi = $"{discordCommunityUrl}/api-teamsaber";
#elif (DISCORDCOMMUNITY)
        private static string discordCommunityApi = $"{discordCommunityUrl}/api";
#elif (ASIAVR)
        private static string discordCommunityApi = $"{discordCommunityUrl}/api-asiavr";
#elif (TRUEACCURACY)
        private static string discordCommunityApi = $"{discordCommunityUrl}/api-acc";
#elif (QUALIFIER)
        private static string discordCommunityApi = $"{discordCommunityUrl}/api-qualifiers";
#elif BTH
        private static string discordCommunityApi = $"{discordCommunityUrl}/api-bth";
#elif BETA
        private static string discordCommunityApi = $"{discordCommunityUrl}/api-beta";
#elif !BETA
        private static string discordCommunityApi = $"{discordCommunityUrl}/api";
#endif

        private static string beatSaverDownloadUrl = "https://beatsaver.com/api/download/hash/";
        //private static string beatSaverDownloadUrl = "http://bsaber.com/dlsongs/";

        [Obfuscation(Exclude = false, Feature = "-rename;")] //This method is called through reflection, so
#if true //BETA
        public static void SubmitScore(ulong userId, string levelId, int difficultyLevel, string characteristic, bool fullCombo, int score, string signed, int playerOptions, int gameOptions, Action<bool> scoreUploadedCallback = null)
#else
        public static void a(ulong userId, string levelId, int difficultyLevel, bool fullCombo, int score, string signed, int playerOptions, int gameOptions, Action<bool> scoreUploadedCallback = null)
#endif
        {
            //Build score object
            Score s = new Score(userId.ToString(), levelId, score, difficultyLevel, fullCombo, playerOptions, gameOptions, characteristic, signed);

            JSONObject o = new JSONObject();
            o.Add("pb", new JSONString(s.ToBase64()));

            SharedCoroutineStarter.instance.StartCoroutine(PostCoroutine(o.ToString(), $"{discordCommunityApi}/submit/", scoreUploadedCallback));
        }

        [Obfuscation(Exclude = false, Feature = "-rename;")] //This method is called through reflection, so
#if BETA
        public static void SubmitRankRequest(ulong userId, string requestedTeamId, string ostInfo, bool initialRequest, string signed, Action<bool> rankRequestedCallback = null)
#else
        public static void b(ulong userId, string requestedTeamId, string ostInfo, bool initialRequest, string signed, Action<bool> rankRequestedCallback = null)
#endif
        {
            //Build score object
            RankRequest s = new RankRequest(userId.ToString(), requestedTeamId, ostInfo, initialRequest, signed);

            JSONObject o = new JSONObject();
            o.Add("pb", new JSONString(s.ToBase64()));

            SharedCoroutineStarter.instance.StartCoroutine(PostCoroutine(o.ToString(), $"{discordCommunityApi}/requestrank/", rankRequestedCallback));
        }

        //Post a score to the server
        private static IEnumerator PostCoroutine(string data, string address, Action<bool> postCompleteCallback = null)
        {
            UnityWebRequest www = UnityWebRequest.Post(address, data);
            www.timeout = 30;
            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
            {
                Logger.Error(www.error);
                postCompleteCallback?.Invoke(false);
            }
            else
            {
                postCompleteCallback?.Invoke(true);
            }
        }

        //Gets the top 10 scores for a song and posts them to the provided leaderboard
        public static void GetSongLeaderboard(CustomLeaderboardController clc, string songHash, LevelDifficulty difficulty, string characteristic, string teamId, bool useTeamColors = false)
        {
            SharedCoroutineStarter.instance.StartCoroutine(GetSongLeaderboardCoroutine(clc, songHash, difficulty, characteristic, teamId, useTeamColors));
        }

        //Starts the necessary coroutine chain to make the mod functional
        public static void GetData(
            SongListViewController slvc,
            string userId,
            Action<Player> userDataGottenCallback = null,
            Action<List<Team>> teamsGottenCallback = null,
            Action<List<Song>> songsGottenCallback = null
            )
        {
            SharedCoroutineStarter.instance.StartCoroutine(GetAllData(slvc, userId, userDataGottenCallback, teamsGottenCallback, songsGottenCallback));
        }

        //Gets all relevant data for the mod to work
        private static IEnumerator GetAllData(
            SongListViewController slvc,
            string userId,
            Action<Player> userDataGottenCallback = null,
            Action<List<Team>> teamsGottenCallback = null,
            Action<List<Song>> songsGottenCallback = null
            )
        {
            yield return SharedCoroutineStarter.instance.StartCoroutine(GetUserData(slvc, userId, userDataGottenCallback));
            yield return SharedCoroutineStarter.instance.StartCoroutine(GetTeams(slvc, teamsGottenCallback));
            if (!slvc.errorHappened && !slvc.HasSongs()) yield return SharedCoroutineStarter.instance.StartCoroutine(GetSongs(slvc, userId, songsGottenCallback));
        }

        //GET the user's profile data from the server
        private static IEnumerator GetUserData(SongListViewController slvc, string userId, Action<Player> userDataGottenCallback = null)
        {
            UnityWebRequest www = UnityWebRequest.Get($"{discordCommunityApi}/playerstats/{userId}");
            Logger.Debug($"GETTING PLAYER DATA: {discordCommunityApi}/playerstats/{userId}");
            www.timeout = 30;
            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
            {
                Logger.Error($"Error getting player stats: {www.error}");
                slvc.ErrorHappened($"Error getting player stats: {www.error}");
            }
            else
            {
                try
                {
                    var node = JSON.Parse(www.downloadHandler.text);

                    //If there is a message from the server, display it
                    if (node["message"] != null && node["message"].ToString().Length > 1)
                    {
                        slvc.ErrorHappened(node["message"]);
                        yield break;
                    }

                    //If the client is out of date, show update message
                    if (VersionCode < Convert.ToInt32(node["version"].Value))
                    {
                        slvc.ErrorHappened($"Version {SharedConstructs.Version} is now out of date. Please download the newest one from the Discord.");
                    }

                    Player.Instance.Team = node["team"];
                    Player.Instance.Tokens = Convert.ToInt32(node["tokens"].Value);
                    Player.Instance.ServerOptions = (ServerFlags)Convert.ToInt32(node["serverSettings"].Value);

                    userDataGottenCallback?.Invoke(Player.Instance);
                }
                catch (Exception e)
                {
                    Logger.Error($"Error parsing playerstats data: {e}");
                    slvc.ErrorHappened($"Error parsing playerstats data: {e}");
                }
            }
        }

        private static IEnumerator GetSongLeaderboardCoroutine(CustomLeaderboardController clc, string songHash, LevelDifficulty difficulty, string characteristic, string teamId = "-1", bool useTeamColors = false)
        {
            UnityWebRequest www = UnityWebRequest.Get($"{discordCommunityApi}/leaderboards/{songHash}/{(int)difficulty}/{characteristic}/{teamId}");
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
                            score.Value["team"]
                        ));

                        //If one of the scores is us, set the "special" score position to the right value
                        if (score.Value["userId"] == Convert.ToString(Plugin.UserId))
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

        //GET the teams from the server
        private static IEnumerator GetTeams(SongListViewController slvc, Action<List<Team>> teamsGottenCallback = null)
        {
            UnityWebRequest www = UnityWebRequest.Get($"{discordCommunityApi}/teams/");
            Logger.Debug($"REQUESTING TEAMS: {discordCommunityApi}/teams/");
            www.timeout = 30;
            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
            {
                Logger.Error($"Error getting teams: {www.error}");
                slvc.ErrorHappened($"Error getting teams: {www.error}");
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
                        var teamObject = 
                            new Team(
                                team.Key,
                                team.Value["teamName"],
                                team.Value["captainId"],
                                team.Value["color"],
                                team.Value["requiredTokens"],
                                team.Value["nextPromotion"]
                            );
                        Team.allTeams.Add(teamObject);
                    }

                    teamsGottenCallback?.Invoke(Team.allTeams);
                }
                catch (Exception e)
                {
                    Logger.Error($"Error parsing teams data: {e}");
                    slvc.ErrorHappened($"Error parsing teams data: {e}");
                    yield break;
                }
            }
        }

        //GET the songs from the server, then start the Download coroutine to download and display them
        //TODO: Time complexity here is a mess.
        private static IEnumerator GetSongs(SongListViewController slvc, string userId, Action<List<Song>> songsGottenCallback = null)
        {
            UnityWebRequest www = UnityWebRequest.Get($"{discordCommunityApi}/songs/{userId}/");
            Logger.Debug($"REQUESTING SONGS: {discordCommunityApi}/songs/{userId}/");

            www.timeout = 30;
            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
            {
                Logger.Error($"Error getting songs: {www.error}");
                slvc.ErrorHappened($"Error getting songs: {www.error}");
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
                            Hash = id.Value["songHash"],
                            SongName = id.Value["songName"],
                            GameOptions = (GameOptions)Convert.ToInt32(id.Value["gameOptions"].ToString()),
                            PlayerOptions = (PlayerOptions)Convert.ToInt32(id.Value["playerOptions"].ToString()),
                            Difficulty = (LevelDifficulty)Convert.ToInt32(id.Value["difficulty"].ToString()),
                            Characteristic = id.Value["characteristic"]
                        };

                        Logger.Debug($"ADDING SONG: {newSong.SongName} {newSong.Difficulty} {newSong.Characteristic}");
                        songs.Add(newSong);
                    }
                }
                catch (Exception e)
                {
                    Logger.Error($"Error parsing getsong data: {e}");
                    slvc.ErrorHappened($"Error parsing getsong data: {e}");
                    yield break;
                }

                //If we got songs, filter them as neccessary then download any we don't have
                List<Song> availableSongs = new List<Song>();

                //Filter out songs we already have and OSTS
                IEnumerable<Song> osts = songs.Where(x => OstHelper.IsOst(x.Hash));
                IEnumerable<Song> alreadyHave = songs.Where(x => Collections.songWithHashPresent(x.Hash.ToUpper()));

                //Loads a level from a song instance, populates the Beatmap property and adds to the available list
                Action<Song> loadLevel = (song) =>
                {
                    if (Collections.songWithHashPresent(song.Hash.ToUpper()))
                    {
                        var levelId = Collections.levelIDsForHash(song.Hash).First();

                        var customPreview = Loader.CustomLevelsCollection.beatmapLevels.First(x => x.levelID == levelId) as CustomPreviewBeatmapLevel;

                        song.PreviewBeatmap = customPreview;

                        //TODO: Figure out proper async-ness here
                        /*var beatmapLevelResult = Task.Run(async () => await SongUtils.GetLevelFromPreview(customPreview));
                        beatmapLevelResult.Wait();

                        //TODO: add characteristic name field to the song data stored in the server
                        song.Beatmap = SongUtils.GetClosestDifficultyPreferLower(beatmapLevelResult.Result?.beatmapLevel, (BeatmapDifficulty)song.Difficulty);
                        availableSongs.Add(song);*/
                    }
                    else
                    {
                        slvc.ErrorHappened($"Could not load level {song.SongName}");
                    }
                };

                //Load the preview levels for what we have
                foreach (Song song in osts)
                {
                    foreach (IBeatmapLevelPack pack in Loader.BeatmapLevelsModelSO.allLoadedBeatmapLevelPackCollection.beatmapLevelPacks)
                    {
                        var foundLevel = pack.beatmapLevelCollection.beatmapLevels.FirstOrDefault(y => y.levelID.ToLower() == song.Hash.ToLower());
                        if (foundLevel != null)
                        {
                            song.PreviewBeatmap = foundLevel;
                        }
                    }
                }

                foreach (Song song in alreadyHave) loadLevel(song);

                //Of what we already have, add the Levels to the availableSongs list
                availableSongs.AddRange(alreadyHave);
                availableSongs.AddRange(osts);

                //Remove what we already have from the download queue
                songs.RemoveAll(x => availableSongs.Contains(x)); //Don't redownload

                //Download the things we don't have, or if we have everything, show the menu
                if (songs.Count > 0)
                {
                    List<IEnumerator> downloadCoroutines = new List<IEnumerator>();
                    songs.ForEach(x =>
                    {
                        downloadCoroutines.Add(DownloadSongs(x.Hash, slvc));
                    });

                    //Wait for the all downloads to finish
                    yield return SharedCoroutineStarter.instance.StartCoroutine(new ParallelCoroutine().ExecuteCoroutines(downloadCoroutines.ToArray()));

                    Action<Loader, Dictionary<string, CustomPreviewBeatmapLevel>> songsLoaded =
                        (_, __) =>
                        {
                            //Now that they're refreshed, we can populate their beatmaps and add them to the available list
                            songs.ForEach(x => loadLevel(x));
                            songsGottenCallback?.Invoke(availableSongs.Union(songs).ToList());
                        };

                    Loader.SongsLoadedEvent -= songsLoaded;
                    Loader.SongsLoadedEvent += songsLoaded;
                    Loader.Instance.RefreshSongs(false);
                }
                else
                {
                    songsGottenCallback?.Invoke(availableSongs);
                }
            }
        }

        //Download songs. Taken from BeatSaberMultiplayer
        //availableSongs: List of IBeatmapLevel which may hold levels already approved for display
        //downloadQueue: List of beatsaver ids representing songs left to download
        //completedDownloads: List of beatsaver ids representing songs that have successfully downloaded
        //songId: The song this instance of the Coroutine is supposed to download
        //slvc: The song list view controller to display the downloaded songs to
        private static IEnumerator DownloadSongs(string songHash, SongListViewController slvc)
        {
            UnityWebRequest www = UnityWebRequest.Get($"{beatSaverDownloadUrl}{songHash}");
#if BETA
            Logger.Info($"DOWNLOADING: {beatSaverDownloadUrl}{songHash}");
#endif
            bool timeout = false;
            float time = 0f;

            www.SetRequestHeader("user-agent", @"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/51.0.2704.106 Safari/537.36");
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
                Logger.Error($"Error downloading song {songHash}: {www.error}");
                slvc.ErrorHappened($"Error downloading song {songHash}: {www.error}");
            }
            else
            {
                //Logger.Info("Received response from BeatSaver.com...");

                string zipPath = "";
                string customSongsPath = CustomLevelPathHelper.customLevelsDirectoryPath;
                string customSongPath = "";

                byte[] data = www.downloadHandler.data;

                try
                {
                    customSongPath = customSongsPath + "/" + songHash + "/";
                    zipPath = customSongPath + songHash + ".zip";
                    if (!Directory.Exists(customSongPath))
                    {
                        Directory.CreateDirectory(customSongPath);
                    }
                    File.WriteAllBytes(zipPath, data);
                    //Logger.Info("Downloaded zip file!");
                }
                catch (Exception e)
                {
                    Logger.Error($"Error writing zip: {e}");
                    slvc.ErrorHappened($"Error writing zip: {e}");
                    yield break;
                }

                //Logger.Info("Extracting...");

                try
                {
                    ZipFile.ExtractToDirectory(zipPath, customSongPath);
                }
                catch (Exception e)
                {
                    Logger.Error($"Unable to extract ZIP! Exception: {e}");
                    slvc.ErrorHappened($"Unable to extract ZIP! Exception: {e}");
                    yield break;
                }

                try
                {
                    File.Delete(zipPath);
                }
                catch (IOException e)
                {
                    Logger.Warning($"Unable to delete zip! Exception: {e}");
                    slvc.ErrorHappened($"Unable to delete zip! Exception: {e}");
                    yield break;
                }

                Logger.Success($"Downloaded!");
            }
        }
    }
}
