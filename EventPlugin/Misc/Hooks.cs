using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Logger = TeamSaberShared.Logger;

/*
 * Created by Moon on 9/23/2018
 * Handles interactions with other mods. Such as multiplayer.
 */
 
namespace TeamSaberPlugin.Misc
{
    [Obfuscation(Exclude = false, Feature = "+rename(mode=decodable,renPdb=true)")]
    class Hooks
    {
        /*
        private static Redirection _currentRedirect;
        private static object _currentOnlineControllerObject;
        private static int _lastScoreSubmitted;

        public static IEnumerator WaitForMultiplayerLevelComplete()
        {
            if (_currentRedirect != null || !ReflectionUtil.ListLoadedAssemblies().Any(x => x.GetName().Name == "BeatSaberMultiplayer")) yield break;

            Type onlineControllerType = ReflectionUtil.GetStaticType("BeatSaberMultiplayer.InGameOnlineController, BeatSaberMultiplayer");
            yield return new WaitUntil(() => Resources.FindObjectsOfTypeAll(onlineControllerType).Any());

            _currentOnlineControllerObject = Resources.FindObjectsOfTypeAll(onlineControllerType).First();

            MethodInfo original = onlineControllerType.GetMethod("SongFinished", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo modified = typeof(Hooks).GetMethod(nameof(a), BindingFlags.Public | BindingFlags.Instance);
            _currentRedirect = new Redirection(original, modified, true);
        }

        [Obfuscation(Exclude = false, Feature = "-rename;")]
        public void a(MainGameSceneSetupData b, LevelCompletionResults e)
        {
            _currentRedirect.InvokeOriginal(_currentOnlineControllerObject, b, e);
            if (e != null && e.levelEndStateType == LevelCompletionResults.LevelEndStateType.Cleared && b.gameplayOptions.validForScoreUse)
            {
                if (e.score == _lastScoreSubmitted) return; //Hacky fix to a callback bug in Multiplayer (SongFinished is called multiple times)
                _lastScoreSubmitted = e.score;

                IBeatmapLevel level = b.difficultyLevel.level;
                string songHash = level.levelID.Substring(0, Math.Min(32, level.levelID.Length));
                string songId = null;

                //If the song is an OST, just send the hash
                if (DiscordCommunityShared.OstHelper.IsOst(songHash))
                {
                    songId = songHash;
                }
                else
                {
                    songId = SongIdHelper.GetSongIdFromLevelId(level.levelID);
                }

                //Community leaderboards
                var d = b.GetProperty("difficultyLevel");
                var g = b.GetProperty("gameplayMode");
                var rs = e.GetProperty("score");
                var fc = e.GetProperty("fullCombo");
                var ms = typeof(DiscordCommunityShared.RSA);
                var s = ms.InvokeMethod("SignScore", Plugin.PlayerId, songId, d.GetProperty<int>("difficulty"), (int)g, fc, rs);

                var c = typeof(Misc.Client);
                var cs = c.InvokeMethod("SubmitScore", Plugin.PlayerId, songId, d.GetProperty<int>("difficulty"), (int)g, fc, rs, s);

                //Real leaderboards                    
                var l = d.GetProperty("level").GetProperty("levelID");
                var ld = d.GetProperty("difficulty");
                var tp = typeof(PersistentSingleton<GameDataModel>);
                var p = tp.GetProperty<GameDataModel>("instance").GetProperty("gameDynamicData").InvokeMethod("GetCurrentPlayerDynamicData").InvokeMethod("GetPlayerLevelStatsData", l, ld, g);
                p.InvokeMethod("IncreaseNumberOfGameplays");

                var gt = typeof(GameplayModeMethods);
                var f = gt.InvokeMethod("IsSolo", g);
                var go = b.GetProperty("gameplayOptions");
                var v = go.GetProperty("validForScoreUse");
                if ((bool)f && (bool)v)
                {
                    p.InvokeMethod("UpdateScoreData", rs, e.maxCombo, e.rank);
                    var pt = typeof(PersistentSingleton<PlatformLeaderboardsModel>);
                    var i = pt.GetProperty<PlatformLeaderboardsModel>("instance");

                    var fe = i.InvokeMethod("IsValidForGameplayMode", g);
                    if ((bool)fe)
                    {
                        var lt = typeof(LeaderboardsModel);
                        var li = lt.InvokeMethod("GetLeaderboardID", d, g);
                        i.InvokeMethod("AddScore", li, e.score);
                    }
                }
            }
        }

        /*
        private IEnumerator WaitForLevelSelection()
        {
            yield return new WaitUntil(() => Resources.FindObjectsOfTypeAll<StandardLevelSelectionFlowCoordinator>().Any());
            Logger.Error("FLOWCOORDINATOR CREATED, ADDING/REMOVING CALLBACKS");
            StandardLevelSelectionFlowCoordinator slsfc = Resources.FindObjectsOfTypeAll<StandardLevelSelectionFlowCoordinator>().First();
            StandardLevelListViewController slsvc = slsfc.GetField<StandardLevelListViewController>("_levelListViewController");
            StandardLevelDifficultyViewController sldvc = slsfc.GetField<StandardLevelDifficultyViewController>("_levelDifficultyViewController");
            slsvc.didSelectLevelEvent -= slsfc.HandleLevelListViewControllerDidSelectLevel;
            sldvc.didSelectDifficultyEvent -= slsfc.HandleDifficultyViewControllerDidSelectDifficulty;
            slsvc.didSelectLevelEvent += HandleLevelListViewControllerDidSelectLevel;
            sldvc.didSelectDifficultyEvent += HandleDifficultyViewControllerDidSelectDifficulty;
        }

        public void HandleLevelListViewControllerDidSelectLevel(StandardLevelListViewController viewController, IBeatmapLevel level)
        {
            Logger.Warning("LEVEL SELECTED");
        }

        public void HandleDifficultyViewControllerDidSelectDifficulty(StandardLevelDifficultyViewController viewController, IDifficultyBeatmap difficultyLevel)
        {
            Logger.Warning("DIFFICULTY SELECTED");
        }

        private IEnumerator WaitForBuildMode()
        {
            yield return new WaitUntil(() => Resources.FindObjectsOfTypeAll<GameBuildMode>().Any());
            Logger.Error("SETTING BUILD TO DEMO");
            GameBuildMode buildMode = Resources.FindObjectsOfTypeAll<GameBuildMode>().First();
            buildMode.ForceSetMode(GameBuildMode.Mode.Demo);
        }
        */
    }
}
