using HMUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

/*
 * Created by Moon on 10/28/2018 at 2:18am
 * Intended to extend the functionality of LeaderboardTableView
 * so we can set custom colors on all the players
 */

namespace DiscordCommunityPlugin.UI.ViewControllers
{
    [Obfuscation(Exclude = false, Feature = "+rename(mode=decodable,renPdb=true)")]
    class CustomLeaderboardTableView : LeaderboardTableView
    {
        private static CustomLeaderboardTableView _instance;

        //Called on object creation (only once in lifetime)
        [Obfuscation(Exclude = false, Feature = "-rename;")]
        public void Awake()
        {
            if (_instance != this)
            {
                _instance = this;

                var existingLeaderboard = Instantiate(Resources.FindObjectsOfTypeAll<LeaderboardTableView>().First());
                _tableView = existingLeaderboard.GetField<TableView>("_tableView");
                _cellPrefab = existingLeaderboard.GetField<LeaderboardTableCell>("_cellPrefab");
                _rowHeight = existingLeaderboard.GetField<float>("_rowHeight");
            }
        }
    }
}
