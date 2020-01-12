using EventShared;
using SongCore;
using System;
using System.Linq;
using System.Reflection;
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
        public static IDifficultyBeatmap GetClosestDifficultyPreferLower(IBeatmapLevel level, BeatmapDifficulty difficulty, BeatmapCharacteristicSO characteristic = null)
        {
            //First, look at the characteristic parameter. If there's something useful in there, we try to use it, but fall back to Standard
            var desiredCharacteristic = level.previewDifficultyBeatmapSets.FirstOrDefault(x => x.beatmapCharacteristic.serializedName == (characteristic?.serializedName ?? "Standard")).beatmapCharacteristic ?? level.previewDifficultyBeatmapSets.First().beatmapCharacteristic;

            IDifficultyBeatmap[] availableMaps =
                level
                .beatmapLevelData
                .difficultyBeatmapSets
                .FirstOrDefault(x => x.beatmapCharacteristic.serializedName == desiredCharacteristic.serializedName)
                .difficultyBeatmaps
                .OrderBy(x => x.difficulty)
                .ToArray();

            IDifficultyBeatmap ret = availableMaps.FirstOrDefault(x => x.difficulty == difficulty);

            if (ret is CustomDifficultyBeatmap)
            {
                var extras = Collections.RetrieveExtraSongData(ret.level.levelID);
                var requirements = extras?._difficulties.First(x => x._difficulty == ret.difficulty).additionalDifficultyData._requirements;
                Logger.Debug($"{ret.level.songName} is a custom level, checking for requirements on {ret.difficulty}...");
                if (
                    (requirements?.Count() > 0) &&
                    (!requirements?.ToList().All(x => Collections.capabilities.Contains(x)) ?? false)
                ) ret = null;
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
            if (ret is CustomDifficultyBeatmap)
            {
                var extras = Collections.RetrieveExtraSongData(ret.level.levelID);
                var requirements = extras?._difficulties.First(x => x._difficulty == ret.difficulty).additionalDifficultyData._requirements;
                Logger.Debug($"{ret.level.songName} is a custom level, checking for requirements on {ret.difficulty}...");
                if (
                    (requirements?.Count() > 0) &&
                    (!requirements?.ToList().All(x => Collections.capabilities.Contains(x)) ?? false)
                ) ret = null;
                Logger.Debug((ret == null ? "Requirement not met." : "Requirement met!"));
            }
            return ret;
        }

        //Returns the next-highest difficulty to the one provided
        private static IDifficultyBeatmap GetHigherDifficulty(IDifficultyBeatmap[] availableMaps, BeatmapDifficulty difficulty, BeatmapCharacteristicSO characteristic)
        {
            var ret = availableMaps.SkipWhile(x => x.difficulty < difficulty).FirstOrDefault();
            if (ret is CustomDifficultyBeatmap)
            {
                var extras = Collections.RetrieveExtraSongData(ret.level.levelID);
                var requirements = extras?._difficulties.First(x => x._difficulty == ret.difficulty).additionalDifficultyData._requirements;
                Logger.Debug($"{ret.level.songName} is a custom level, checking for requirements on {ret.difficulty}...");
                if (
                    (requirements?.Count() > 0) &&
                    (!requirements?.ToList().All(x => Collections.capabilities.Contains(x)) ?? false)
                ) ret = null;
                Logger.Debug((ret == null ? "Requirement not met." : "Requirement met!"));
            }
            return ret;
        }

        public static async Task<bool> HasDLCLevel(string levelId, AdditionalContentModel additionalContentModel = null)
        {
            additionalContentModel = additionalContentModel ?? Resources.FindObjectsOfTypeAll<AdditionalContentModel>().FirstOrDefault();
            if (additionalContentModel != null)
            {
                getStatusCancellationTokenSource?.Cancel();
                getStatusCancellationTokenSource = new CancellationTokenSource();

                var token = getStatusCancellationTokenSource.Token;
                return await additionalContentModel.GetLevelEntitlementStatusAsync(levelId, token) == AdditionalContentModel.EntitlementStatus.Owned;
            }

            return false;
        }

        public static async Task<BeatmapLevelsModel.GetBeatmapLevelResult?> GetLevelFromPreview(IPreviewBeatmapLevel level, BeatmapLevelsModel beatmapLevelsModel = null)
        {
            beatmapLevelsModel = beatmapLevelsModel ?? Resources.FindObjectsOfTypeAll<BeatmapLevelsModel>().FirstOrDefault();

            if (beatmapLevelsModel != null)
            {
                getLevelCancellationTokenSource?.Cancel();
                getLevelCancellationTokenSource = new CancellationTokenSource();

                var token = getLevelCancellationTokenSource.Token;

                BeatmapLevelsModel.GetBeatmapLevelResult? result = null;
                try
                {
                    result = await beatmapLevelsModel.GetBeatmapLevelAsync(level.levelID, token);
                }
                catch (OperationCanceledException) { }
                if (result?.isError == true || result?.beatmapLevel == null) return null; //Null out entirely in case of error
                return result;
            }
            return null;
        }

        public static string GetHashFromLevelId(string levelId)
        {
            if (OstHelper.IsOst(levelId)) return levelId;
            return Collections.hashForLevelID(levelId).ToLower();
        }
    }
}
