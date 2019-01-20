using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

/**
 * Created by Moon on 1/20/2019
 * Represents a "team"
 * A list of these is downloaded by the plugin with player data and song data
 */

namespace TeamSaberPlugin.DiscordCommunityHelpers
{
    class Team
    {
        public static List<Team> allTeams = new List<Team>();

        public string TeamId { get; private set; }
        public string TeamName { get; private set; }
        public string CaptainId { get; private set; }
        public Color Color { get; private set; }

        public Team(string teamId, string teamName, string captainId, string color)
        {
            TeamId = teamId;
            TeamName = teamName;
            CaptainId = captainId;

            ColorUtility.TryParseHtmlString(color, out var parsedColor);
            Color = parsedColor;
        }
    }
}