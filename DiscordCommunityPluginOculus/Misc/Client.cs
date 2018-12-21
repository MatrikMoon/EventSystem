using ChristmasVotePlugin.UI.ViewControllers;
using ChristmasShared;
using ChristmasShared.SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;
using static ChristmasShared.SharedConstructs;
using Logger = ChristmasShared.Logger;

/*
 * Created by Moon on 9/9/2018
 * Communicates with a running DiscordCommunityServer
 */

namespace ChristmasVotePlugin.Misc
{
    [Obfuscation(Exclude = false, Feature = "+rename(mode=decodable,renPdb=true)")]
    class Client
    {
        private static string discordCommunityUrl = "https://networkauditor.org";
#if DEBUG
        private static string discordCommunityApi = $"{discordCommunityUrl}/api-christmas-beta";
#else
        private static string discordCommunityApi = $"{discordCommunityUrl}/api-christmas";
#endif
        private static string beatSaverDownloadUrl = "https://beatsaver.com/download/";
        //private static string beatSaverDownloadUrl = "http://bsaber.com/dlsongs/";

        public static void SubmitVote(ulong userId, string itemId, Category category, string signed, Action<bool> voteSubmittedCallback = null)
        {
            //Build score object
            Vote s = new Vote
            {
                UserId = userId.ToString(),
                ItemId = itemId,
                Category = (int)category,
                Signed = signed
            };

            byte[] voteData = ProtobufHelper.SerializeProtobuf(s);

            SharedCoroutineStarter.instance.StartCoroutine(SubmitVoteCoroutine(voteData, voteSubmittedCallback));
        }

        //Post a score to the server
        private static IEnumerator SubmitVoteCoroutine(byte[] proto, Action<bool> voteSubmittedCallback = null)
        {
            JSONObject o = new JSONObject();
            o.Add("pb", new JSONString(Convert.ToBase64String(proto)));

            UnityWebRequest www = UnityWebRequest.Post($"{discordCommunityApi}/submitvote/", o.ToString());
            www.timeout = 30;
            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
            {
                Logger.Error(www.error);
                voteSubmittedCallback?.Invoke(false);
            }
            else
            {
                Logger.Success("Vote submission complete!");
                voteSubmittedCallback?.Invoke(true);
            }
        }

        //Starts the necessary coroutine chain to make the mod functional
        public static void GetEventData(LevelCollectionSO lcfgm, ItemListViewController slvc, string userId, Category category = Category.None)
        {
            SharedCoroutineStarter.instance.StartCoroutine(GetAllData(lcfgm, slvc, userId));
        }

        public static void GetUserData(ItemListViewController slvc, string userId)
        {
            SharedCoroutineStarter.instance.StartCoroutine(GetUserDataCoroutine(slvc, userId));
        }

        //Gets votes for a particular user and hands that data off to the song list view controller
        public static IEnumerator GetUserDataCoroutine(ItemListViewController slvc, string userId)
        {
            UnityWebRequest www = UnityWebRequest.Get($"{discordCommunityApi}/getuserdata/{userId}");
#if DEBUG
            Logger.Info($"REQUESTING USER DATA: {discordCommunityApi}/getuserdata/{userId}");
#endif
            www.timeout = 30;
            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
            {
                Logger.Error($"Error getting user data: {www.error}");
                slvc.DownloadErrorHappened($"Error getting user data: {www.error}");
            }
            else
            {
                List<TableItem> items = new List<TableItem>();
                try
                {
                    //Get the list of items the user has voted on
                    var node = JSON.Parse(www.downloadHandler.text);
                    foreach (var item in node)
                    {
                        items.Add(new TableItem
                        {
                            Name = item.Value["name"],
                            Author = item.Value["author"],
                            SubName = item.Value["subName"],
                            Category = (Category)Convert.ToInt32(item.Value["category"].ToString()),
                            ItemId = item.Key
                        });
                    }
                    slvc.VotedOn = items;
                }
                catch (Exception e)
                {
                    Logger.Error($"Error parsing user data: {e}");
                    slvc.DownloadErrorHappened($"Error parsing user data: {e}");
                    yield break;
                }
            }
        }

        //Gets all relevant data for the mod to work
        private static IEnumerator GetAllData(LevelCollectionSO lcfgm, ItemListViewController slvc, string userId, Category category = Category.None)
        {
            yield return SharedCoroutineStarter.instance.StartCoroutine(
                new ParallelCoroutine().ExecuteCoroutines(
                    new IEnumerator[] { GetUserDataCoroutine(slvc, userId), GetItemsForCategory(lcfgm, slvc, category) }
                )
            );
        }

        //GET the weekly songs from the server, then start the Download coroutine to download and display them
        //TODO: Time complexity here is a mess.
        private static IEnumerator GetItemsForCategory(LevelCollectionSO lcfgm, ItemListViewController slvc, Category category = Category.None)
        {
            UnityWebRequest www = UnityWebRequest.Get($"{discordCommunityApi}/getitems/{(category != Category.None ? $"{(int)category}" : "")}");
#if DEBUG
            Logger.Info($"REQUESTING ITEMS: {discordCommunityApi}/getitems/{(category != Category.None ? $"{(int)category}" : "")}");
#endif
            www.timeout = 30;
            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
            {
                Logger.Error($"Error getting items: {www.error}");
                slvc.DownloadErrorHappened($"Error getting items: {www.error}");
            }
            else
            {
                List<TableItem> items = new List<TableItem>();
                try
                {
                    //Get the list of songs to download, and map out the song ids to the corresponding gamemodes
                    var node = JSON.Parse(www.downloadHandler.text);
                    foreach (var item in node)
                    {
                        items.Add(new TableItem
                        {
                            Name = item.Value["name"],
                            Author = item.Value["author"],
                            SubName = item.Value["subName"],
                            Category = (Category)Convert.ToInt32(item.Value["category"].ToString()),
                            ItemId = item.Key
                        });
                        slvc.SetItems(items);
                    }
                }
                catch (Exception e)
                {
                    Logger.Error($"Error parsing item data: {e}");
                    slvc.DownloadErrorHappened($"Error parsing item data: {e}");
                    yield break;
                }

                /*
                //If we got songs, filter them as neccessary then download any we don't have
                List<IBeatmapLevel> availableSongs = new List<IBeatmapLevel>();

                //Filter out songs we already have and OSTS
                IEnumerable<string> alreadyHave = items.Where(x => SongIdHelper.GetSongExistsBySongId(x.ItemId)).Select(x => x.ItemId);

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

                //Remove the id's of what we already have
                items.RemoveAll(x => alreadyHave.Contains(x)); //Don't redownload

                //Download the things we don't have, or if we have everything, show the menu
                if (items.Count > 0)
                {
                    List<IEnumerator> downloadCoroutines = new List<IEnumerator>();
                    items.ForEach(x =>
                    {
                        downloadCoroutines.Add(DownloadItem(x, slvc));
                    });

                    //Wait for the all downloads to finish
                    yield return SharedCoroutineStarter.instance.StartCoroutine(new ParallelCoroutine().ExecuteCoroutines(downloadCoroutines.ToArray()));

                    Action<SongLoader, List<SongLoaderPlugin.OverrideClasses.CustomLevel>> songsLoaded =
                        (SongLoader sender, List<SongLoaderPlugin.OverrideClasses.CustomLevel> loadedSongs) =>
                        {
                            //Now that they're refreshed, we can add them to the available list
                            items.ForEach(x => availableSongs.Add(SongIdHelper.GetLevelFromSongId(x)));

                            slvc.SetItems(availableSongs);
                        };

                    SongLoader.SongsLoadedEvent -= songsLoaded;
                    SongLoader.SongsLoadedEvent += songsLoaded;
                    SongLoader.Instance.RefreshSongs(false);
                }
                else
                {
                    slvc.SetItems(availableSongs);
                }
                */
            }
        }

        //Download files. Taken from BeatSaberMultiplayer
        //slvc: The song list view controller to display errors to
        private static IEnumerator DownloadItem(string songId, ItemListViewController slvc = null)
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
                slvc?.DownloadErrorHappened($"Error downloading song {songId}: {www.error}");
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
                    slvc?.DownloadErrorHappened($"Error writing zip: {e}");
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
                    slvc?.DownloadErrorHappened($"Unable to extract ZIP! Exception: {e}");
                    yield break;
                }

                try
                {
                    File.Delete(zipPath);
                }
                catch (IOException e)
                {
                    Logger.Warning($"Unable to delete zip! Exception: {e}");
                    slvc?.DownloadErrorHappened($"Unable to delete zip! Exception: {e}");
                    yield break;
                }

                Logger.Success($"Downloaded!");
            }
        }
    }
}