using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.IO;
using TeamSaberShared;
using static TeamSaberShared.SharedConstructs;

/*
 * Created by Moon on 9/11/2018
 * Helper, assisting in interacting with the database
 * TODO: Use Linq for SQL
 */

namespace TeamSaberServer.Database
{
    public class SimpleSql
    {
        private static string databaseLocation = Directory.GetCurrentDirectory();
        private static string databaseName = "TeamSaberDatabase";
        private static string databaseExtension = "db";
        private static string databaseFullPath = $@"{databaseLocation}\{databaseName}.{databaseExtension}";

        private static SQLiteConnection OpenConnection()
        {
            SQLiteConnection dbc = null;
            if (!File.Exists(databaseFullPath))
            {
                SQLiteConnection.CreateFile($"{databaseName}.{databaseExtension}");
                dbc = new SQLiteConnection($"Data Source={databaseName}.{databaseExtension};Version=3;");
                dbc.Open();

                ExecuteCommand("CREATE TABLE IF NOT EXISTS playerTable (_id INTEGER PRIMARY KEY AUTOINCREMENT, steamId TEXT DEFAULT '', discordName TEXT DEFAULT '', discordExtension TEXT DEFAULT '', discordMention TEXT DEFAULT '', timezone TEXT DEFAULT '', rarity INTEGER DEFAULT 0, team TEXT DEFAULT '', rank INTEGER DEFAULT 0, totalScore BIGINT DEFAULT 0, topScores BIGINT DEFAULT 0, songsPlayed INTEGER DEFAULT 0, personalBestsBeaten INTEGER DEFAULT 0, playersBeat INTEGER DEFAULT 0, mentionMe BIT DEFAULT 0, liquidated BIT DEFAULT 0)");
                ExecuteCommand("CREATE TABLE IF NOT EXISTS scoreTable (_id INTEGER PRIMARY KEY AUTOINCREMENT, songId TEXT DEFAULT '', steamId TEXT DEFAULT '', rarity INTEGER DEFAULT 0, team TEXT DEFAULT '', difficultyLevel INTEGER DEFAULT 0, fullCombo BIT DEFAULT 0, score BIGINT DEFAULT 0, old BIT DEFAULT 0)");
                ExecuteCommand("CREATE TABLE IF NOT EXISTS songTable (_id INTEGER PRIMARY KEY AUTOINCREMENT, songName TEXT DEFAULT '', songAuthor TEXT DEFAULT '', songSubtext TEXT DEFAULT '', songId TEXT DEFAULT '', difficulty INTEGER DEFAULT 0, old BIT DEFAULT 0)");
                ExecuteCommand("CREATE TABLE IF NOT EXISTS teamTable (_id INTEGER PRIMARY KEY AUTOINCREMENT, teamId TEXT DEFAULT '', teamName TEXT DEFAULT '', captainId TEXT DEFAULT '', color TEXT DEFAULT '', score INTEGER DEFAULT 0, old BIT DEFAULT 0)");
            }
            else
            {
                dbc = new SQLiteConnection($"Data Source={databaseName}.{databaseExtension};Version=3;");
                dbc.Open();
            }

            return dbc;
        }

        public static int ExecuteCommand(string command)
        {
            SQLiteConnection db = OpenConnection();
            SQLiteCommand c = new SQLiteCommand(command, db);
            int ret = c.ExecuteNonQuery();
            db.Close();
            return ret;
        }

        public static List<string> ExecuteQuery(string query, string columnToReturn)
        {
            List<string> ret = new List<string>();
            SQLiteConnection db = OpenConnection();
            using (SQLiteCommand command = new SQLiteCommand(query, db))
            {
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        ret.Add(reader[columnToReturn]?.ToString());
                    }
                }
            }

            db.Close();

            return ret;
        }

        public static bool AddPlayer(string steamId, string discordName, string discordExtension, string discordMention, string timezone, int rartiy, string team, int rank, long totalScore, long topScores, int songsPlayed, int personalBestsBeaten, int playersBeat, bool mentionMe)
        {
            return ExecuteCommand($"INSERT INTO playerTable VALUES (NULL, \'{steamId}\', \'{discordName}\', \'{discordExtension}\', \'{discordMention}\', \'{timezone}\', {rartiy}, \'{team}\', {rank}, {totalScore}, {topScores}, {songsPlayed}, {personalBestsBeaten}, {playersBeat}, {(mentionMe ? "1" : "0")}, 0)") > 0;
        }

        public static bool AddScore(string songId, string steamId, int rarity, string team, LevelDifficulty levelDifficulty, bool fullCombo, long score)
        {
            return ExecuteCommand($"INSERT INTO scoreTable VALUES (NULL, \'{songId}\', \'{steamId}\', {rarity}, \'{team}\', {(int)levelDifficulty}, {(fullCombo ? 1: 0)}, {score}, 0)") > 0;
        }

        public static bool AddSong(string songName, string songAuthor, string songSubtext, string songId, LevelDifficulty difficulty)
        {
            return ExecuteCommand($"INSERT INTO songTable VALUES (NULL, \'{songName}\', \'{songAuthor}\', \'{songSubtext}\', \'{songId}\', {(int)difficulty}, 0)") > 0;
        }

        public static bool AddTeam(string teamId, string teamName, string captainId, string color, int score)
        {
            return ExecuteCommand($"INSERT INTO teamTable VALUES (NULL, \'{teamId}\', \'{teamName}\', \'{captainId}\', \'{color}\', {score}, 0)") > 0;
        }

        //This marks all songs and scores as "old"
        public static int MarkAllOld()
        {
            int ret = 0;
            ret += ExecuteCommand("UPDATE songTable SET old = 1");
            ret += ExecuteCommand("UPDATE scoreTable SET old = 1");
            return ret;
        }

        //Returns a list of all the teams
        public static List<Team> GetAllTeams()
        {
            List<Team> ret = new List<Team>();
            SQLiteConnection db = OpenConnection();
            using (SQLiteCommand command = new SQLiteCommand("SELECT teamId FROM teamTable WHERE NOT old = 1", db))
            {
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        ret.Add(new Team(reader["teamId"].ToString()));
                    }
                }
            }

            db.Close();

            return ret;
        }

        //Returns a list of all the teams
        public static List<Player> GetAllPlayers()
        {
            List<Player> ret = new List<Player>();
            SQLiteConnection db = OpenConnection();
            using (SQLiteCommand command = new SQLiteCommand($"SELECT steamId FROM playerTable WHERE NOT liquidated = 1", db))
            {
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        ret.Add(new Player(reader["steamId"].ToString()));
                    }
                }
            }

            db.Close();

            return ret;
        }

        //Returns a list of SongConstruct of the currently active songs
        public static List<SongConstruct> GetActiveSongs()
        {
            List<SongConstruct> ret = new List<SongConstruct>();
            SQLiteConnection db = OpenConnection();
            using (SQLiteCommand command = new SQLiteCommand("SELECT songId, songName, difficulty FROM songTable WHERE NOT old = 1", db))
            {
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        SongConstruct item = new SongConstruct()
                        {
                            SongId = reader["songId"].ToString(),
                            Name = reader["songName"].ToString(),
                            Difficulty = (LevelDifficulty)Convert.ToInt32(reader["difficulty"].ToString())
                        };
                        ret.Add(item);
                    }
                }
            }

            db.Close();

            return ret;
        }

        //These are in here and not the Scores.cs file because in here, we have direct access to the database and
        //can grab what we need in less queries
        //Returns a dictionary of <SongConstruct, Dictionary<steamId, ScoreConstruct>>
        public static IDictionary<SongConstruct, IDictionary<string, ScoreConstruct>> GetAllActiveScoresForFilterOrTeam(Rarity r, Team t)
        {
            List<SongConstruct> songs = GetActiveSongs();

            Dictionary<SongConstruct, IDictionary<string, ScoreConstruct>> scores = new Dictionary<SongConstruct, IDictionary<string, ScoreConstruct>>();
            songs.ForEach(x => {
                string songId = x.SongId;
                scores.Add(x, GetScoresForSong(x, (long)r, t.GetTeamId()));
            });

            return scores;
        }

        //Returns a dictionary of steamIds and scores for the designated song and rarity
        public static IDictionary<string, ScoreConstruct> GetScoresForSong(SongConstruct s, long rarity = (long)Rarity.All, string teamId = "-1")
        {
            Dictionary<string, ScoreConstruct> ret = new Dictionary<string, ScoreConstruct>();
            SQLiteConnection db = OpenConnection();
            using (SQLiteCommand command = new SQLiteCommand($"SELECT steamId, score, fullCombo, difficultyLevel, rarity, team FROM scoreTable WHERE songId = \'{s.SongId}\' AND difficultyLevel = {(int)s.Difficulty} {((Rarity)rarity != Rarity.All ? $"AND rarity = \'{rarity}\'" : null)} {(teamId != "-1" ? $"AND team = \'{teamId}\'" : null)} AND NOT old = 1 ORDER BY score DESC", db))
            {
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        ret.Add(
                            reader["steamId"].ToString(),
                            new ScoreConstruct
                            {
                                Score = Convert.ToInt64(reader["score"].ToString()),
                                FullCombo = reader["fullCombo"].ToString() == "True",
                                Rarity = (Rarity)Convert.ToInt64(reader["rarity"].ToString()),
                                TeamId = reader["team"].ToString(),
                                Difficulty = (LevelDifficulty)Convert.ToInt64(reader["difficultyLevel"].ToString())
                            });
                    }
                }
            }

            db.Close();

            return ret;
        }

        //Returns a dictionary of SongConstructs and scores for the designated song and rarity
        public static IDictionary<SongConstruct, ScoreConstruct> GetScoresForPlayer(string steamId)
        {
            Dictionary<SongConstruct, ScoreConstruct> ret = new Dictionary<SongConstruct, ScoreConstruct>();
            SQLiteConnection db = OpenConnection();
            using (SQLiteCommand command = new SQLiteCommand($"SELECT score, songId, rarity, team, difficultyLevel, fullCombo FROM scoreTable WHERE steamId = \'{steamId}\' AND NOT old = 1", db))
            {
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        ret.Add(
                            new SongConstruct()
                            {
                                SongId = reader["songId"].ToString(),
                                Difficulty = (LevelDifficulty)Convert.ToInt32(reader["difficultyLevel"].ToString())
                            },
                            new ScoreConstruct {
                                Score = Convert.ToInt64(reader["score"].ToString()),
                                FullCombo = reader["fullCombo"].ToString() == "True",
                                Rarity = (Rarity)Convert.ToInt64(reader["rarity"].ToString()),
                                TeamId = reader["team"].ToString(),
                                Difficulty = (LevelDifficulty)Convert.ToInt64(reader["difficultyLevel"].ToString())
                            });
                    }
                }
            }

            db.Close();

            return ret;
        }

        //Tiny classes to help organize leaderboard commands and reduce strain on SQL
        public class ScoreConstruct
        {
            public long Score { get; set; }
            public bool FullCombo { get; set; }
            public Rarity Rarity { get; set; }
            public string TeamId { get; set; }
            public LevelDifficulty Difficulty { get; set; }
        }

        public class SongConstruct
        {
            public string SongId { get; set; }
            public string Name { get; set; }
            public LevelDifficulty Difficulty { get; set; }

            //Necessary overrides for being used as a key in a Dictionary
            public static bool operator ==(SongConstruct a, SongConstruct b)
            {
                if (b == null) return false;
                return a.GetHashCode() == b.GetHashCode();
            }

            public static bool operator !=(SongConstruct a, SongConstruct b)
            {
                if (b == null) return false;
                return a.GetHashCode() != b.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                if (obj == null) return false;
                if (!(obj is SongConstruct)) return false;
                return GetHashCode() == obj.GetHashCode();
            }

            public override int GetHashCode()
            {
                int hash = 13;
                hash = (hash * 7) + SongId.GetHashCode();
                hash = (hash * 7) + Difficulty.GetHashCode();
                return hash;
            }
            //End necessary overrides
        }
    }
}
