using CustomUI.BeatSaber;
using DiscordCommunityShared;
using System;
using System.Reflection;
using UnityEngine.UI;
using VRUI;

namespace ChristmasVotePlugin.UI.ViewControllers
{
    [Obfuscation(Exclude = false, Feature = "+rename(mode=decodable,renPdb=true)")]
    class GeneralNavigationController : VRUINavigationController
    {
        private Button _backButton;
        public event Action<GeneralNavigationController> didFinishEvent;

        [Obfuscation(Exclude = false, Feature = "-rename;")]
        protected override void DidActivate(bool firstActivation, ActivationType activationType)
        {
            if (firstActivation && activationType == ActivationType.AddedToHierarchy)
            {
                _backButton = BeatSaberUI.CreateBackButton(rectTransform);
                _backButton.onClick.AddListener(() => DismissButtonWasPressed());
            }
        }

        public virtual void DismissButtonWasPressed()
        {
            didFinishEvent?.Invoke(this);
        }
    }
}
