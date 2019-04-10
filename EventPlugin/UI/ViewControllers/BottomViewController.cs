using System.Reflection;
using UnityEngine;
using VRUI;

/**
 * Created by Moon on 3/21/2019, 11:59pm
 */

namespace EventPlugin.UI.ViewControllers
{
    [Obfuscation(Exclude = false, Feature = "+rename(mode=decodable,renPdb=true)")]
    class BottomViewController : VRUIViewController
    {
        [Obfuscation(Exclude = false, Feature = "-rename;")]
        protected override void DidActivate(bool firstActivation, ActivationType activationType)
        {
            if (firstActivation && activationType == ActivationType.AddedToHierarchy)
            {
                var noFail = EventUI.instance.noFailController;
                var noFailTransform = noFail.transform as RectTransform;
                noFailTransform.SetParent(rectTransform, false);
                noFailTransform.anchorMin = new Vector2(0.5f, 0.5f);
                noFailTransform.anchorMax = new Vector2(0.5f, 0.5f);
                noFailTransform.anchoredPosition = new Vector2(-20f, 5f);
                noFailTransform.sizeDelta = new Vector2(40, noFailTransform.sizeDelta.y);

                noFail.applyImmediately = true;
                noFail.GetValue += () =>
                {
                    var communityLeaderboard = EventUI.instance._mainModFlowCoordinator._communityLeaderboard;

                    return communityLeaderboard.selectedSong.GameOptions.HasFlag(EventShared.SharedConstructs.GameOptions.NoFail);
                };
                noFail.SetValue += (b) =>
                {
                    var communityLeaderboard = EventUI.instance._mainModFlowCoordinator._communityLeaderboard;
                    
                    if (b) communityLeaderboard.selectedSong.GameOptions |= EventShared.SharedConstructs.GameOptions.NoFail;
                    else communityLeaderboard.selectedSong.GameOptions &= ~EventShared.SharedConstructs.GameOptions.NoFail;
                };
            }
        }
    }
}
