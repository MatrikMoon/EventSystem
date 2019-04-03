using CustomUI.BeatSaber;
using EventPlugin.Helpers;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using VRUI;

/**
 * Created by Moon on 3/28/2019
 * Allows a user to select a team they want to sabotage
 */

namespace EventPlugin.UI.ViewControllers
{
    [Obfuscation(Exclude = false, Feature = "+rename(mode=decodable,renPdb=true)")]
    class TeamSelectionViewController : VRUIViewController
    {
        public event Action<string> TeamSelected;

        [Obfuscation(Exclude = false, Feature = "-rename;")]
        protected override void DidActivate(bool firstActivation, ActivationType activationType)
        {
            if (firstActivation && activationType == ActivationType.AddedToHierarchy)
            {
                var teams = Team.allTeams.OrderBy(x => x.Score).ToList();

                float buttonWidth = 50f;
                float buttonHeight = 10f;
                float horizontalPadding = 30f;
                float verticalPadding = 10f;

                for (int x = 0; x < teams.Count; x++)
                {
                    float xCoord = (x % 3) * buttonWidth + horizontalPadding;
                    float yCoord = (x / 3) * buttonHeight + verticalPadding;
                    var currentTeam = teams[x];

                    var button = BeatSaberUI.CreateUIButton(rectTransform, "CreditsButton", buttonText: $"{currentTeam.TeamName}: {currentTeam.Score}");
                    var buttonTransform = (button.transform as RectTransform);
                    buttonTransform.anchorMin = new Vector2(0f, 0f);
                    buttonTransform.anchorMax = new Vector2(0f, 0f);
                    buttonTransform.sizeDelta = new Vector2(buttonWidth, buttonHeight);
                    buttonTransform.anchoredPosition = new Vector2(xCoord, yCoord);
                    button.onClick.AddListener(() => TeamSelected?.Invoke(currentTeam.TeamId));
                }
            }
        }
    }
}
