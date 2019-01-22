using TeamSaberShared;
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

namespace TeamSaberServer.Database
{
    public class Team
    {
        private string teamId;

        public Team(string teamId)
        {
            this.teamId = teamId;
            if (!Exists())
            {
                SimpleSql.AddTeam(teamId, "", "", "");
            }
        }
        
        public string GetTeamId()
        {
            return teamId;
        }

        public string GetTeamName()
        {
            return SimpleSql.ExecuteQuery($"SELECT teamName FROM teamTable WHERE teamId = \'{teamId}\'", "teamName").First();
        }

        public bool SetTeamName(string name)
        {
            name = Regex.Replace(name, "[^a-zA-Z0-9]", "");
            return SimpleSql.ExecuteCommand($"UPDATE teamTable SET teamName = \'{name}\' WHERE teamId = \'{teamId}\'") > 1;
        }

        public string GetCaptain()
        {
            return SimpleSql.ExecuteQuery($"SELECT captainId FROM teamTable WHERE teamId = \'{teamId}\'", "captainId").First();
        }

        public bool SetCaptain(string userId)
        {
            return SimpleSql.ExecuteCommand($"UPDATE teamTable SET captainId = \'{userId}\' WHERE teamId = \'{teamId}\'") > 1;
        }

        public string GetColor()
        {
            return SimpleSql.ExecuteQuery($"SELECT color FROM teamTable WHERE teamId = \'{teamId}\'", "color").First();
        }

        public bool SetColor(string color)
        {
            color = Regex.Replace(color, "[^a-zA-Z0-9#]", "");
            return SimpleSql.ExecuteCommand($"UPDATE teamTable SET color = \'{color}\' WHERE teamId = \'{teamId}\'") > 1;
        }

        public bool IsOld()
        {
            return SimpleSql.ExecuteQuery($"SELECT old FROM teamTable WHERE teamId = \'{teamId}\'", "old").First() == "1";
        }

        public bool Exists()
        {
            return Exists(teamId);
        }

        public static bool Exists(string teamId)
        {
            teamId = Regex.Replace(teamId, "[^a-zA-Z0-9]", "");
            return SimpleSql.ExecuteQuery($"SELECT * FROM teamTable WHERE teamId = \'{teamId}\'", "teamId").Any();
        }
    }
}
