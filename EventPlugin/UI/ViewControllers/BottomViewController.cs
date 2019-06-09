using CustomUI.BeatSaber;
using EventPlugin.Misc;
using EventPlugin.Models;
using EventShared;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VRUI;

/**
 * Created by Moon on 3/21/2019, 11:59pm
 */

namespace EventPlugin.UI.ViewControllers
{
    [Obfuscation(Exclude = false, Feature = "+rename(mode=decodable,renPdb=true)")]
    class BottomViewController : VRUIViewController
    {
        private Player _player;
        private List<Team> _teams;

        private Button _rankUpButton;
        private TextMeshProUGUI _tokensText;

        [Obfuscation(Exclude = false, Feature = "-rename;")]
        protected override void DidActivate(bool firstActivation, ActivationType activationType)
        {
            if (firstActivation && activationType == ActivationType.AddedToHierarchy)
            {
                _tokensText = BeatSaberUI.CreateText(rectTransform, "Tokens: XX", Vector2.zero);
                _tokensText.rectTransform.anchorMin = new Vector2(0.3f, 0.5f);
                _tokensText.rectTransform.anchorMax = new Vector2(0.3f, 0.5f);
                _tokensText.fontSize = 8f;

                _rankUpButton = BeatSaberUI.CreateUIButton(rectTransform, "QuitButton", () =>
                {
                    var currentTeam = _teams.First(x => x.TeamId == _player.Team);
                    var nextTeam = _teams.First(x => x.TeamId == currentTeam.NextPromotion);

                    var s = RSA.SignRankRequest(Plugin.PlayerId, nextTeam.TeamId, false);
#if BETA
                    var n = "SubmitRankRequest";
#else
                    var n = "b";
#endif
                    typeof(Client).InvokeMethod(n, Plugin.PlayerId, nextTeam.TeamId, "[NOT YET IMPLEMENTED]", false, s, null);
                }, "Rank Up");
                (_rankUpButton.transform as RectTransform).anchorMin = new Vector2(0.7f, 0.5f);
                (_rankUpButton.transform as RectTransform).anchorMax = new Vector2(0.7f, 0.5f);
                (_rankUpButton.transform as RectTransform).anchoredPosition = Vector2.zero;
                (_rankUpButton.transform as RectTransform).sizeDelta = new Vector2(50, 10);
                _rankUpButton.interactable = false;

                UpdateUI();
            }
        }

        public void SetPlayer(Player player)
        {
            _player = player;
            UpdateUI();
        }

        public void SetTeams(List<Team> teams)
        {
            _teams = teams;
            UpdateUI();
        }

        private void UpdateUI()
        {
            if (_tokensText != null && _rankUpButton != null)
            {
                if (_player != null) _tokensText.SetText($"Tokens: {_player.Tokens}");
                if (_player != null && _teams != null)
                {
                    var currentTeam = _teams.First(x => x.TeamId == _player.Team);
                    var nextTeam = _teams.First(x => x.TeamId == currentTeam.NextPromotion);
                    _rankUpButton.interactable = _player.Tokens >= nextTeam.RequiredTokens;
                }
            }
        }
    }
}
