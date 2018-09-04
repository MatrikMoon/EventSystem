using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VRUI;

/*
 * Created by Moon on 8/23/2018
 * This is the class for the Modal which would appear when
 * the user doesn't have the mods required for this plugin
 */

namespace DiscordCommunityPlugin.UI
{
    class MainModNavigationController : VRUINavigationController
    {
        public TextMeshProUGUI _errorText;

        private Button _backButton;

        protected override void DidActivate(bool firstActivation, ActivationType activationType)
        {
            if (firstActivation && activationType == ActivationType.AddedToHierarchy)
            {
                _backButton = BaseUI.CreateBackButton(rectTransform);
                _backButton.onClick.AddListener(delegate () { CommunityUI._instance._mainFlowCooridnator.LeaveMod(); });

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
