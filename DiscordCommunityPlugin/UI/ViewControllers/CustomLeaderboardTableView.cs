using DiscordCommunityPlugin.DiscordCommunityHelpers;
using HMUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using static DiscordCommunityShared.SharedConstructs;
using Logger = DiscordCommunityShared.Logger;

/*
 * Created by Moon on 10/28/2018 at 2:18am
 * Intended to extend the functionality of LeaderboardTableView
 * so we can set custom colors on all the players
 */

namespace DiscordCommunityPlugin.UI.ViewControllers
{
    [Obfuscation(Exclude = false, Feature = "+rename(mode=decodable,renPdb=true)")]
    class CustomLeaderboardTableView : MonoBehaviour, TableView.IDataSource
    {
        protected int _specialScorePos;
        protected float _rowHeight = 5f;

        private TableView _tableView;
        private LeaderboardTableCell _cellInstance;
        private List<CustomScoreData> _scores;
        private bool _useRankColors = false;

        [Obfuscation(Exclude = false, Feature = "-rename;")]
        public void Awake()
        {
            _tableView = new GameObject().AddComponent<TableView>();
            _tableView.transform.SetParent(transform, false);

            var currentView = Resources.FindObjectsOfTypeAll<LeaderboardTableView>().First();
            var currentTransform = (currentView.transform as RectTransform);
            var newTransform = (_tableView.transform as RectTransform);

            newTransform.anchorMin = new Vector2(currentTransform.anchorMin.x, currentTransform.anchorMin.y);
            newTransform.anchorMax = new Vector2(currentTransform.anchorMax.x, currentTransform.anchorMax.y);
            newTransform.anchoredPosition = new Vector2(currentTransform.anchoredPosition.x, currentTransform.anchoredPosition.y);
            newTransform.sizeDelta = new Vector2(currentTransform.sizeDelta.x - 40, currentTransform.sizeDelta.y);

            _cellInstance = Resources.FindObjectsOfTypeAll<LeaderboardTableCell>().First(x => x.name == "LeaderboardTableCell");
        }

        public TableCell CellForRow(int row)
        {
            LeaderboardTableCell leaderboardTableCell = Instantiate(_cellInstance);
            leaderboardTableCell.reuseIdentifier = "Cell";

            CustomScoreData scoreData = _scores[row];
            leaderboardTableCell.rank = scoreData.rank;
            leaderboardTableCell.playerName = scoreData.playerName;
            leaderboardTableCell.score = scoreData.score;
            leaderboardTableCell.showFullCombo = scoreData.fullCombo;
            leaderboardTableCell.showSeparator = (row != _scores.Count - 1);
            leaderboardTableCell.specialScore = (_specialScorePos == row);
            if (!(_specialScorePos == row) && _useRankColors) leaderboardTableCell.GetField<TextMeshProUGUI>("_playerNameText").color = Player.GetColorForRank(scoreData.CommunityRank);
            return leaderboardTableCell;
        }

        public int NumberOfRows()
        {
            if (_scores == null)
            {
                return 0;
            }
            return _scores.Count;
        }

        public float RowHeight()
        {
            return _rowHeight;
        }

        public virtual void SetScores(List<CustomScoreData> scores, int specialScorePos, bool useRankColors = false)
        {
            _scores = scores;
            _specialScorePos = specialScorePos;
            _useRankColors = useRankColors;
            if (_tableView.dataSource == null)
            {
                _tableView.dataSource = this;
            }
            else
            {
                _tableView.ReloadData();
            }
        }

        public class CustomScoreData : LeaderboardTableView.ScoreData
        {
            public Rank CommunityRank
            {
                get;
                private set;
            }

            public CustomScoreData(int score, string playerName, int place, bool fullCombo, Rank rank = Rank.None) : base(score, playerName, place, fullCombo)
            {
                CommunityRank = rank;
            }
        }
    }
}
