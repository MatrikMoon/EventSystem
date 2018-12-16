using CustomUI.BeatSaber;
using System;
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
        public event Action<TableItem> VotePressed;
        public TableItem SelectedItem { get; private set; }

        Button _voteButton;

        [Obfuscation(Exclude = false, Feature = "-rename;")]
        protected override void DidActivate(bool firstActivation, ActivationType type)
        {
            if (firstActivation && type == ActivationType.AddedToHierarchy)
            {
                _voteButton = BeatSaberUI.CreateUIButton(rectTransform, "CreditsButton");
                _voteButton.SetButtonText("Vote");
                (_voteButton.transform as RectTransform).anchorMin = new Vector2(.5f, .5f);
                (_voteButton.transform as RectTransform).anchorMax = new Vector2(.5f, .5f);
                (_voteButton.transform as RectTransform).anchoredPosition = new Vector2(0, 0);
                (_voteButton.transform as RectTransform).sizeDelta = new Vector2(35f, 35f);
                _voteButton.onClick.AddListener(() => VotePressed?.Invoke(SelectedItem));
            }
            else if (!firstActivation && type == ActivationType.AddedToHierarchy)
            {
                //Disable relevant views
                _voteButton.gameObject.SetActive(false);
            }
        }

        public void SetItem(TableItem item, ItemListViewController ilvc = null)
        {
            //Set globals
            SelectedItem = item;

            //Enable relevant views
            _voteButton.gameObject.SetActive(true);

            if (ilvc != null)
            {
                if (ilvc.VotedOn.Contains(item))
                {
                    _voteButton.interactable = false;
                    _voteButton.SetButtonText("Voted");
                }
                else
                {
                    _voteButton.interactable = true;
                    _voteButton.SetButtonText("Vote");
                }
            }
        }

        public void Refresh()
        {
            if (SelectedItem != null) SetItem(SelectedItem);
        }
    }
}
