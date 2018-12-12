using ChristmasVotePlugin.Misc;
using CustomUI.BeatSaber;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using VRUI;
using static DiscordCommunityShared.SharedConstructs;

/*
 * Created by Moon on 9/22/2018
 * A ViewController allowing me to show a leaderboard and other player data
 */

namespace ChristmasVotePlugin.UI.ViewControllers
{
    [Obfuscation(Exclude = false, Feature = "+rename(mode=decodable,renPdb=true)")]
    class CustomLeftViewController : VRUIViewController
    {
        public event Action<string, Category> VotePressed;
        public string SelectedItem { get; private set; }
        public Category SelectedCategory { get; private set; } = Category.None;

        Button _rankUpButton;

        [Obfuscation(Exclude = false, Feature = "-rename;")]
        protected override void DidActivate(bool firstActivation, ActivationType type)
        {
            if (firstActivation && type == ActivationType.AddedToHierarchy)
            {
                _rankUpButton = BeatSaberUI.CreateUIButton(rectTransform, "CreditsButton");
                _rankUpButton.SetButtonText("Vote");
                (_rankUpButton.transform as RectTransform).anchorMin = new Vector2(.5f, .5f);
                (_rankUpButton.transform as RectTransform).anchorMax = new Vector2(.5f, .5f);
                (_rankUpButton.transform as RectTransform).sizeDelta = new Vector2(35f, 35f);
                _rankUpButton.onClick.AddListener(() => VotePressed?.Invoke(SelectedItem, SelectedCategory));
            }
            else if (!firstActivation && type == ActivationType.AddedToHierarchy)
            {
                //Disable relevant views
                _rankUpButton.gameObject.SetActive(false);

                SelectedCategory = Category.None;
            }
        }

        public void SetItem(string itemId, Category category)
        {
            //Set globals
            SelectedItem = itemId;
            SelectedCategory = category;

            //Enable relevant views
            _rankUpButton.gameObject.SetActive(true);

            //Get player's data
            //This is left over from the community plugin, and repurposed to disable the vote button if the user has
            //already voted for this item
            Client.GetUserData(this, Plugin.PlayerId);
        }

        public void Refresh()
        {
            if (SelectedItem != null && SelectedCategory != Category.None) SetItem(SelectedItem, SelectedCategory);
        }

        public void SetScores(List<CustomLeaderboardTableView.CustomScoreData> scores, int myScorePos, bool useRankColors = false)
        {
            int num = (scores != null) ? scores.Count : 0;
            for (int j = num; j < 10; j++)
            {
                scores.Add(new CustomLeaderboardTableView.CustomScoreData(-1, string.Empty, j + 1, false));
            }

            _leaderboard.SetScores(scores, myScorePos, useRankColors);
        }
    }
}
