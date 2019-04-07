using CustomUI.BeatSaber;
using System;
using System.Reflection;
using UnityEngine.UI;
using VRUI;

namespace EventPlugin.UI.ViewControllers
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
                _backButton = BeatSaberUI.CreateBackButton(rectTransform, () => didFinishEvent?.Invoke(this));
            }
        }
    }
}
