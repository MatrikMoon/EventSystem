using EventPlugin.Misc;
using EventPlugin.Models;
using EventPlugin.UI.Views;
using HMUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/*
 * Created by Moon on 9/22/2018
 * A ViewController allowing me to show a leaderboard and other player data
 */

namespace EventPlugin.UI.ViewControllers
{
    [Obfuscation(Exclude = false, Feature = "+rename(mode=decodable,renPdb=true)")]
    class CustomLeaderboardController : ViewController
    {
        protected CustomLeaderboardTableView _leaderboard;

        [Obfuscation(Exclude = false, Feature = "-rename;")]
        public Song selectedSong;

        public event Action<Song> PlayPressed;
        public string selectedTeam = "-1";
        public int selectedTeamIndex = -1;

        TextMeshProUGUI _difficulty;
        TextMeshProUGUI _songName;
        TextMeshProUGUI _team;
        Button _playButton;
        Button _pageLeftButton;
        Button _pageRightButton;

        [Obfuscation(Exclude = false, Feature = "-rename;")]
        protected override void DidActivate(bool firstActivation, ActivationType type)
        {
            if (firstActivation && type == ActivationType.AddedToHierarchy)
            {
                gameObject.SetActive(false);
                _leaderboard = gameObject.AddComponent<CustomLeaderboardTableView>();
                _leaderboard.transform.SetParent(rectTransform, false);
                _leaderboard.name = "Community Leaderboard";
                gameObject.SetActive(true);

                _difficulty = BeatSaberUI.CreateText(rectTransform, "Difficulty", new Vector2());
                _difficulty.fontSize = 3f;
                _difficulty.alignment = TextAlignmentOptions.Center;
                (_difficulty.transform as RectTransform).anchorMin = new Vector2(.5f, 1f);
                (_difficulty.transform as RectTransform).anchorMax = new Vector2(.5f, 1f);
                (_difficulty.transform as RectTransform).anchoredPosition = new Vector2(0f, -5f);

                _songName = BeatSaberUI.CreateText(rectTransform, "Song", new Vector2());
                _songName.fontSize = 8f;
                _songName.alignment = TextAlignmentOptions.Center;
                (_songName.transform as RectTransform).anchorMin = new Vector2(.5f, 1f);
                (_songName.transform as RectTransform).anchorMax = new Vector2(.5f, 1f);
                (_songName.transform as RectTransform).anchoredPosition = new Vector2(0f, -10f);

                _team = BeatSaberUI.CreateText(rectTransform, "Team", new Vector2());
                _team.fontSize = 4f;
                _team.alignment = TextAlignmentOptions.Center;
                _team.color = Color.gray;
                (_team.transform as RectTransform).anchorMin = new Vector2(.5f, 1f);
                (_team.transform as RectTransform).anchorMax = new Vector2(.5f, 1f);
                (_team.transform as RectTransform).anchoredPosition = new Vector2(0f, -15f);

                _pageLeftButton = Instantiate(Resources.FindObjectsOfTypeAll<Button>().First(x => (x.name == "PageUpButton")), rectTransform, false);
                (_pageLeftButton.transform as RectTransform).anchorMin = new Vector2(0f, 0.5f);
                (_pageLeftButton.transform as RectTransform).anchorMax = new Vector2(0f, 0.5f);
                (_pageLeftButton.transform as RectTransform).anchoredPosition = new Vector2(27f, -10f);
                (_pageLeftButton.transform as RectTransform).sizeDelta = new Vector2(40f, 5f);
                Quaternion currentRot = (_pageLeftButton.transform as RectTransform).rotation;
                (_pageLeftButton.transform as RectTransform).rotation = Quaternion.Euler(currentRot.eulerAngles.x, currentRot.eulerAngles.y, currentRot.eulerAngles.z + 90);
                _pageLeftButton.interactable = true;
                _pageLeftButton.onClick.AddListener(() =>
                {
                    SetSong(selectedSong, --selectedTeamIndex);
                    if (selectedTeamIndex <= -1) _pageLeftButton.interactable = false;
                    _pageRightButton.interactable = true;
                });

                _pageRightButton = Instantiate(Resources.FindObjectsOfTypeAll<Button>().First(x => (x.name == "PageDownButton")), rectTransform, false);
                (_pageRightButton.transform as RectTransform).anchorMin = new Vector2(1f, 0.5f);
                (_pageRightButton.transform as RectTransform).anchorMax = new Vector2(1f, 0.5f);
                (_pageRightButton.transform as RectTransform).anchoredPosition = new Vector2(-27f, -10f);
                (_pageRightButton.transform as RectTransform).sizeDelta = new Vector2(40f, 5f);
                currentRot = (_pageRightButton.transform as RectTransform).rotation;
                (_pageRightButton.transform as RectTransform).rotation = Quaternion.Euler(currentRot.eulerAngles.x, currentRot.eulerAngles.y, currentRot.eulerAngles.z + 90);
                _pageRightButton.interactable = true;
                _pageRightButton.onClick.AddListener(() =>
                {
                    SetSong(selectedSong, ++selectedTeamIndex);
                    if (selectedTeamIndex >= Team.allTeams.Count - 1) _pageRightButton.interactable = false;
                    _pageLeftButton.interactable = true;
                });

                _playButton = BeatSaberUI.CreateUIButton(rectTransform, "CreditsButton");
                _playButton.SetButtonText("Play");
                (_playButton.transform as RectTransform).anchorMin = new Vector2(1f, 1f);
                (_playButton.transform as RectTransform).anchorMax = new Vector2(1f, 1f);
                (_playButton.transform as RectTransform).anchoredPosition = new Vector2(-17f, -8f);
                (_playButton.transform as RectTransform).sizeDelta = new Vector2(28f, 10f);
                _playButton.onClick.AddListener(() =>
                {
                    PlayPressed?.Invoke(selectedSong);
                });

                if (selectedTeamIndex <= -1) _pageLeftButton.interactable = false;
                if (selectedTeamIndex >= Team.allTeams.Count - 1) _pageRightButton.interactable = false;

                //???
                //_leaderboard.SetUpArrow(_pageLeftButton);
                //_leaderboard.SetDownArrow(_pageRightButton);
            }
            else if (!firstActivation && type == ActivationType.AddedToHierarchy)
            {
                //Disable relevant views
                _leaderboard.gameObject.SetActive(false);
                _difficulty.gameObject.SetActive(false);
                _songName.gameObject.SetActive(false);
                _team.gameObject.SetActive(false);
                _playButton.gameObject.SetActive(false);
                _pageLeftButton.gameObject.SetActive(false);
                _pageRightButton.gameObject.SetActive(false);

                selectedTeam = "-1";
                selectedTeamIndex = -1;
            }
        }

        public void SetSong(Song song, int teamIndex)
        {
            //Set globals
            selectedSong = song;

            //Load the beatmap

            if (teamIndex >= 0)
            {
                selectedTeam = Team.allTeams.ToArray().ElementAt(teamIndex).TeamId;
            }
            else selectedTeam = "-1";

            //Enable relevant views
            _leaderboard.gameObject.SetActive(true);
            _difficulty.gameObject.SetActive(true);
            _songName.gameObject.SetActive(true);
            _team.gameObject.SetActive(true);
            _playButton.gameObject.SetActive(true);
            _pageLeftButton.gameObject.SetActive(true);
            _pageRightButton.gameObject.SetActive(true);

            //Set song name text and team text (and color)
            _songName.SetText(song.Beatmap.level.songName);
            _difficulty.SetText(song.Beatmap.difficulty.ToString());

            if (selectedTeam == "-1")
            {
                _team.color = Color.green;
                _team.SetText("Mixed");
            }
            else
            {
                var currentTeam = Team.allTeams.FirstOrDefault(x => x.TeamId == selectedTeam);
                _team.color = currentTeam.Color;
                _team.SetText(currentTeam.TeamName);
            }

            //Get leaderboard data
            Client.GetSongLeaderboard(this, song.Hash, song.Difficulty, song.Characteristic, selectedTeam, selectedTeam == "-1");
        }

        public void Refresh()
        {
            if (selectedSong != null) SetSong(selectedSong, selectedTeamIndex);
        }

        public void SetScores(List<CustomLeaderboardTableView.CustomScoreData> scores, int myScorePos, bool useTeamColors = false)
        {
            int num = (scores != null) ? scores.Count : 0;
            for (int j = num; j < 10; j++)
            {
                scores.Add(new CustomLeaderboardTableView.CustomScoreData(-1, string.Empty, j + 1, false));
            }

            _leaderboard.SetScores(scores, myScorePos, useTeamColors);
        }
    }
}
