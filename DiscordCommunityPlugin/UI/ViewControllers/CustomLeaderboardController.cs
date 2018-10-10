using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VRUI;
using static DiscordCommunityShared.SharedConstructs;
using Object = UnityEngine.Object;
using Logger = DiscordCommunityShared.Logger;
using DiscordCommunityPlugin.Misc;

/*
 * Created by Moon on 9/22/2018
 * A ViewController allowing me to show a leaderboard and other player data
 */

namespace DiscordCommunityPlugin.UI.ViewControllers
{
    [Obfuscation(Exclude = false, Feature = "+rename(mode=decodable,renPdb=true)")]
    class CustomLeaderboardController : VRUIViewController
    {
        protected LeaderboardTableView _leaderboard;

        public event Action<GameplayOptions> PlayPressed;

        TextMeshProUGUI _songName;
        TextMeshProUGUI _rank;
        Button _playButton;
        Button _mirrorButton;

        [Obfuscation(Exclude = false, Feature = "-rename;")]
        protected override void DidActivate(bool firstActivation, ActivationType type)
        {
            if (firstActivation && type == ActivationType.AddedToHierarchy)
            {
                _leaderboard = Instantiate(Resources.FindObjectsOfTypeAll<LeaderboardTableView>().First());
                _leaderboard.transform.SetParent(rectTransform, false);
                _leaderboard.name = "Community Leaderboard";
                CleanLeaderboards(_leaderboard.transform);

                _songName = BaseUI.CreateText(rectTransform, "Song", new Vector2(0f, -10f));
                _songName.fontSize = 8f;
                _songName.alignment = TextAlignmentOptions.Center;
                _songName.gameObject.SetActive(false);

                _rank = BaseUI.CreateText(rectTransform, "Rank", new Vector2(0f, -15f));
                _rank.fontSize = 4f;
                _rank.alignment = TextAlignmentOptions.Center;
                _rank.color = Color.gray;
                _rank.gameObject.SetActive(false);

                _playButton = BaseUI.CreateUIButton(rectTransform, "QuitButton");
                BaseUI.SetButtonText(_playButton, "Play");
                (_playButton.transform as RectTransform).anchorMin = new Vector2(1f, 1f);
                (_playButton.transform as RectTransform).anchorMax = new Vector2(1f, 1f);
                (_playButton.transform as RectTransform).anchoredPosition = new Vector2(-21f, -15f);
                (_playButton.transform as RectTransform).sizeDelta = new Vector2(18f, 10f);
                _playButton.onClick.AddListener(() =>
                {
                    PlayPressed?.Invoke(new GameplayOptions());
                });

                _mirrorButton = BaseUI.CreateUIButton(rectTransform, "QuitButton");
                BaseUI.SetButtonText(_mirrorButton, "Mirror");
                (_mirrorButton.transform as RectTransform).anchorMin = new Vector2(1f, 1f);
                (_mirrorButton.transform as RectTransform).anchorMax = new Vector2(1f, 1f);
                (_mirrorButton.transform as RectTransform).anchoredPosition = new Vector2(-21f, -28f);
                (_mirrorButton.transform as RectTransform).sizeDelta = new Vector2(18f, 10f);
                _mirrorButton.onClick.AddListener(() =>
                {
                    GameplayOptions options = new GameplayOptions();
                    options.mirror = true;
                    PlayPressed?.Invoke(options);
                });
            }
        }

        //When cloning existing objects, we can sometimes clone things we don't want
        //Here, we clean out cloned junk items from a leaderboard
        private static void CleanLeaderboards(Transform transform)
        {
            var container = transform.Find("Viewport").Find("Content");

            //An instance of the clones we want to destroy
            var cellClone = container.Find("LeaderboardTableCell(Clone)");

            //Loop through all the clones and destroy all their children
            while (cellClone != null)
            {
                DestroyImmediate(cellClone.gameObject);
                cellClone = container.Find("LeaderboardTableCell(Clone)");
            }
        }

        public void SetSong(IStandardLevelDifficultyBeatmap map)
        {
            //Enable text views
            _songName.gameObject.SetActive(true);
            _rank.gameObject.SetActive(true);

            //Set song name text and rank text (and color)
            Rank playerRank = DiscordCommunityHelpers.Player.Instance.rank;
            _songName.SetText(map.level.songName);
            _rank.SetText(playerRank.ToString());

            if (playerRank == Rank.Silver)
            {
                _rank.color = Color.gray;
            }
            else if (playerRank == Rank.Gold)
            {
                _rank.color = Color.yellow;
            }
            else if (playerRank == Rank.Blue)
            {
                _rank.color = Color.blue;
            }
            else if (playerRank == Rank.Master)
            {
                _rank.color = Color.magenta;
            }

            //Get leaderboard data
            Misc.Client.GetSongLeaderboard(this, Misc.SongIdHelper.GetSongIdFromLevelId(map.level.levelID), DiscordCommunityHelpers.Player.Instance.rank);
        }

        public void SetScores(List<LeaderboardTableView.ScoreData> scores, int myScorePos)
        {
            int num = (scores != null) ? scores.Count : 0;
            for (int j = num; j < 10; j++)
            {
                scores.Add(new LeaderboardTableView.ScoreData(-1, string.Empty, j + 1, fullCombo: false));
            }

            _leaderboard.SetScores(scores, myScorePos);
        }
    }
}
