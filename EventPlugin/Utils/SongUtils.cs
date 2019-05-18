using EventShared;
using SongLoaderPlugin;
using SongLoaderPlugin.OverrideClasses;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Logger = EventShared.Logger;

namespace EventPlugin.Utils
{
    [Obfuscation(Exclude = false, Feature = "+rename(mode=decodable,renPdb=true)")]
    class SongUtils
    {
        private static CancellationTokenSource getLevelCancellationTokenSource;
        private static CancellationTokenSource getStatusCancellationTokenSource;

        //Returns the closest difficulty to the one provided, preferring lower difficulties first if any exist
        public static IDifficultyBeatmap GetClosestDifficultyPreferLower(BeatmapLevelSO level, BeatmapDifficulty difficulty, BeatmapCharacteristicSO characteristic = null)
        {
            //First, look at the characteristic parameter. If there's something useful in there, we try to use it, but fall back to Standard
            var desiredCharacteristic = level.beatmapCharacteristics.FirstOrDefault(x => x.serializedName == (characteristic?.serializedName ?? "Standard")) ?? level.beatmapCharacteristics.First();

            IDifficultyBeatmap[] availableMaps =
                level
                .difficultyBeatmapSets
                .FirstOrDefault(x => x.beatmapCharacteristic.serializedName == desiredCharacteristic.serializedName)
                .difficultyBeatmaps
                .OrderBy(x => x.difficulty)
                .ToArray();

            IDifficultyBeatmap ret = availableMaps.FirstOrDefault(x => x.difficulty == difficulty);

            if (ret is CustomLevel.CustomDifficultyBeatmap)
            {
                Logger.Debug($"{ret.level.songName} is a custom level, checking for requirements on {ret.difficulty}...");
                if ((ret as CustomLevel.CustomDifficultyBeatmap).requirements.Any(x => !SongLoader.capabilities.Contains(x))) ret = null;
                Logger.Debug((ret == null ? "Requirement not met." : "Requirement met!"));
            }

            if (ret == null)
            {
                ret = GetLowerDifficulty(availableMaps, difficulty, desiredCharacteristic);
            }
            if (ret == null)
            {
                ret = GetHigherDifficulty(availableMaps, difficulty, desiredCharacteristic);
            }

            return ret;
        }

        //Returns the next-lowest difficulty to the one provided
        private static IDifficultyBeatmap GetLowerDifficulty(IDifficultyBeatmap[] availableMaps, BeatmapDifficulty difficulty, BeatmapCharacteristicSO characteristic)
        {
            var ret = availableMaps.TakeWhile(x => x.difficulty < difficulty).LastOrDefault();
            if (ret is CustomLevel.CustomDifficultyBeatmap)
            {
                Logger.Debug($"{ret.level.songName} is a custom level, checking for requirements on {ret.difficulty}...");
                if ((ret as CustomLevel.CustomDifficultyBeatmap).requirements.Any(x => !SongLoader.capabilities.Contains(x))) ret = null;
                Logger.Debug((ret == null ? "Requirement not met." : "Requirement met!"));
            }
            return ret;
        }

        //Returns the next-highest difficulty to the one provided
        private static IDifficultyBeatmap GetHigherDifficulty(IDifficultyBeatmap[] availableMaps, BeatmapDifficulty difficulty, BeatmapCharacteristicSO characteristic)
        {
            var ret = availableMaps.SkipWhile(x => x.difficulty < difficulty).FirstOrDefault();
            if (ret is CustomLevel.CustomDifficultyBeatmap)
            {
                Logger.Debug($"{ret.level.songName} is a custom level, checking for requirements on {ret.difficulty}...");
                if ((ret as CustomLevel.CustomDifficultyBeatmap).requirements.Any(x => !SongLoader.capabilities.Contains(x))) ret = null;
                Logger.Debug((ret == null ? "Requirement not met." : "Requirement met!"));
            }
            return ret;
        }

        public static async Task<bool> HasDLCLevel(string levelId, AdditionalContentModelSO additionalContentModel = null)
        {
            additionalContentModel = additionalContentModel ?? Resources.FindObjectsOfTypeAll<AdditionalContentModelSO>().FirstOrDefault();
            var additionalContentHandler = additionalContentModel?.GetField<IPlatformAdditionalContentHandler>("_platformAdditionalContentHandler");

            if (additionalContentHandler != null)
            {
                getStatusCancellationTokenSource?.Cancel();
                getStatusCancellationTokenSource = new CancellationTokenSource();

                var token = getStatusCancellationTokenSource.Token;
                return await additionalContentHandler.GetLevelEntitlementStatusAsync(levelId, token) == AdditionalContentModelSO.EntitlementStatus.Owned;
            }

            return false;
        }

        public static async Task<BeatmapLevelLoader.LoadBeatmapLevelResult?> GetDLCLevel(IPreviewBeatmapLevel level, BeatmapLevelsModelSO beatmapLevelsModel = null)
        {
            beatmapLevelsModel = beatmapLevelsModel ?? Resources.FindObjectsOfTypeAll<BeatmapLevelsModelSO>().FirstOrDefault();
            var beatmapLevelLoader = beatmapLevelsModel?.GetField<BeatmapLevelLoader>("_beatmapLevelLoader");

            if (beatmapLevelLoader != null)
            {
                getLevelCancellationTokenSource?.Cancel();
                getLevelCancellationTokenSource = new CancellationTokenSource();

                var token = getLevelCancellationTokenSource.Token;

                BeatmapLevelLoader.LoadBeatmapLevelResult? result = null;
                try
                {
                    result = await beatmapLevelLoader.LoadBeatmapLevelAsync(level, token);
                }
                catch (OperationCanceledException) { }

                if (result?.isError == true || result?.beatmapLevel == null) return null; //Null out entirely in case of error
                return result;
            }
            return null;
        }

        public static string GetSongIdFromLevelId(string levelId)
        {
            if (OstHelper.IsOst(levelId)) return levelId;

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
