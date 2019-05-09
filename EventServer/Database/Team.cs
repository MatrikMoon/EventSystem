using EventShared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

/*
 * Created by Moon on 9/11/2018
 * TODO: Use Properties (get/set) instead of getters and setters
 */

namespace EventServer.Database
{
    public class Team
    {
        public string TeamId { get; private set; }

        //DiscordCommunity rank progression
        public int RequiredTokens {
            get
            {
                return Convert.ToInt32(SimpleSql.ExecuteQuery($"SELECT requiredTokens FROM teamTable WHERE teamId = \'{TeamId}\'", "requiredTokens").First());
            }
            set
            {
                SimpleSql.ExecuteCommand($"UPDATE teamTable SET requiredTokens = \'{value}\' WHERE teamId = \'{TeamId}\'");
            }
        }
        public string NextPromotion
        {
            get
            {
                return SimpleSql.ExecuteQuery($"SELECT nextPromotion FROM teamTable WHERE teamId = \'{TeamId}\'", "nextPromotion").First();
            }
            set
            {
                SimpleSql.ExecuteCommand($"UPDATE teamTable SET nextPromotion = \'{value}\' WHERE teamId = \'{TeamId}\'");
            }
        }

        public Team(string teamId)
        {
            TeamId = teamId;
            if (!Exists())
            {
                SimpleSql.AddTeam(teamId, "", "", "", 0, "");
            }
        }

        public string TeamName
        {
            get
            {
                return SimpleSql.ExecuteQuery($"SELECT teamName FROM teamTable WHERE teamId = \'{TeamId}\'", "teamName").First();
            }
            set
            {
                var name = Regex.Replace(value, "[^a-zA-Z0-9 ]", "");
                SimpleSql.ExecuteCommand($"UPDATE teamTable SET teamName = \'{name}\' WHERE teamId = \'{TeamId}\'");
            }
        }
        public string Captain
        {
            get
            {
                return SimpleSql.ExecuteQuery($"SELECT captainId FROM teamTable WHERE teamId = \'{TeamId}\'", "captainId").First();
            }
            set
            {
                SimpleSql.ExecuteCommand($"UPDATE teamTable SET captainId = \'{value}\' WHERE teamId = \'{TeamId}\'");
            }
        }
        public string Color
        {
            get
            {
                return SimpleSql.ExecuteQuery($"SELECT color FROM teamTable WHERE teamId = \'{TeamId}\'", "color").First();
            }
            set
            {
                var color = Regex.Replace(value, "[^a-zA-Z0-9#]", "");
                SimpleSql.ExecuteCommand($"UPDATE teamTable SET color = \'{color}\' WHERE teamId = \'{TeamId}\'");
            }
        }

        public bool IsOld() => SimpleSql.ExecuteQuery($"SELECT old FROM teamTable WHERE teamId = \'{TeamId}\'", "old").First() == "1";

        public static Team GetByDiscordMentionOfCaptain(string mention)
        {
            Player player = Player.GetByDiscordMetion(mention);
            if (player == null) return null; //If the player doesn't exist, we definitely don't need to continue; it's obviously an invalid request

            return SimpleSql.GetAllTeams().FirstOrDefault(x => x.Captain== player.SteamId);
        }

        public bool Exists() => Exists(TeamId);
        public static bool Exists(string teamId)
        {
            teamId = Regex.Replace(teamId, "[^a-zA-Z0-9]", "");
            return SimpleSql.ExecuteQuery($"SELECT * FROM teamTable WHERE teamId = \'{teamId}\'", "teamId").Any();
        }
    }
}
