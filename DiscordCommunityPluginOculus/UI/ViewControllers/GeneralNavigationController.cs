using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VRUI;

/*
 * Created by andruzzzhka
 */

namespace DiscordCommunityPlugin.UI.ViewControllers
{
    [Obfuscation(Exclude = false, Feature = "+rename(mode=decodable,renPdb=true)")]
    class GeneralNavigationController : VRUINavigationController
    {
        public TextMeshProUGUI _errorText;

        private Button _backButton;

        [Obfuscation(Exclude = false, Feature = "-rename;")]
        protected override void DidActivate(bool firstActivation, ActivationType activationType)
        {
            if (firstActivation && activationType == ActivationType.AddedToHierarchy)
            {
                _backButton = BaseUI.CreateBackButton(rectTransform);
                _backButton.onClick.AddListener(delegate () { DismissModalViewController(null, false); });

                _errorText = BaseUI.CreateText(rectTransform, "", new Vector2(0f, -25f));
                _errorText.fontSize = 8f;
                _errorText.alignment = TextAlignmentOptions.Center;
                _errorText.rectTransform.sizeDelta = new Vector2(120f, 6f);
            }
            _errorText.text = "";
        }

        public void DisplayError(string error)
        {
            if (_errorText != null)
                _errorText.text = error;
        }
    }
}
