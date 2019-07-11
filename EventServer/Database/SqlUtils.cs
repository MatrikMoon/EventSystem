using EventShared;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using static EventShared.SharedConstructs;

/*
 * Created by Moon on 9/11/2018
 * Helper, assisting in interacting with the database
 * TODO: Use Linq for SQL
 */

namespace EventServer.Database
{
    public class SqlUtils
    {
        private static string databaseLocation = Directory.GetCurrentDirectory();
        private static string databaseName = "EventDatabase";
        private static string databaseExtension = "db";
        private static string databaseFullPath = $@"{databaseLocation}\{databaseName}.{databaseExtension}";
        private static bool initialized = false;

        private static SQLiteConnection OpenConnection()
        {
            SQLiteConnection dbc;
            if (!File.Exists(databaseFullPath))
            {
                SQLiteConnection.CreateFile($"{databaseName}.{databaseExtension}");
                dbc = new SQLiteConnection($"Data Source={databaseName}.{databaseExtension};Version=3;");
                dbc.Open();
            }
            else
            {
                dbc = new SQLiteConnection($"Data Source={databaseName}.{databaseExtension};Version=3;");
                dbc.Open();
            }

            if (!initialized)
            {
                initialized = true;
                ExecuteCommand("CREATE TABLE IF NOT EXISTS playerTable (_id INTEGER PRIMARY KEY AUTOINCREMENT, userId TEXT DEFAULT '', discordName TEXT DEFAULT '', discordExtension TEXT DEFAULT '', discordMention TEXT DEFAULT '', extras TEXT DEFAULT '', team TEXT DEFAULT '', rank INTEGER DEFAULT 0, tokens INTEGER DEFAULT 0, totalScore BIGINT DEFAULT 0, topScores BIGINT DEFAULT 0, songsPlayed INTEGER DEFAULT 0, personalBestsBeaten INTEGER DEFAULT 0, playersBeat INTEGER DEFAULT 0, mentionMe BIT DEFAULT 0, liquidated BIT DEFAULT 0)");
                ExecuteCommand("CREATE TABLE IF NOT EXISTS scoreTable (_id INTEGER PRIMARY KEY AUTOINCREMENT, songHash TEXT DEFAULT '', userId TEXT DEFAULT '', team TEXT DEFAULT '', difficulty INTEGER DEFAULT 0, gameOptions INTEGER DEFAULT 0, playerOptions INTEGER DEFAULT 0, fullCombo BIT DEFAULT 0, score BIGINT DEFAULT 0, old BIT DEFAULT 0)");
                ExecuteCommand("CREATE TABLE IF NOT EXISTS songTable (_id INTEGER PRIMARY KEY AUTOINCREMENT, songName TEXT DEFAULT '', songAuthor TEXT DEFAULT '', songSubtext TEXT DEFAULT '', songHash TEXT DEFAULT '', difficulty INTEGER DEFAULT 0, gameOptions INTEGER DEFAULT 0, playerOptions INTEGER DEFAULT 0, old BIT DEFAULT 0)");
                ExecuteCommand("CREATE TABLE IF NOT EXISTS teamTable (_id INTEGER PRIMARY KEY AUTOINCREMENT, teamId TEXT DEFAULT '', teamName TEXT DEFAULT '', captainId TEXT DEFAULT '', color TEXT DEFAULT '', requiredTokens INTEGER DEFAULT 0, nextPromotion TEXT DEFAULT '', old BIT DEFAULT 0)");
                ExecuteCommand("CREATE TABLE IF NOT EXISTS voteTable (_id INTEGER PRIMARY KEY AUTOINCREMENT, userId TEXT DEFAULT '', messageId BIGINT DEFAULT 0, nextPromotion TEXT DEFAULT '', votes INTEGER DEFAULT 0, requiredVotes INTEGER DEFAULT 0, old BIT DEFAULT 0)");
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

        public static bool AddPlayer(string userId, string discordName, string discordExtension, string discordMention, string extras, string team, int rank, int tokens, long totalScore, long topScores, int songsPlayed, int personalBestsBeaten, int playersBeat, bool mentionMe)
        {
            return ExecuteCommand($"INSERT INTO playerTable VALUES (NULL, \'{userId}\', \'{discordName}\', \'{discordExtension}\', \'{discordMention}\', \'{extras}\', \'{team}\', {rank}, {tokens}, {totalScore}, {topScores}, {songsPlayed}, {personalBestsBeaten}, {playersBeat}, {(mentionMe ? "1" : "0")}, 0)") > 0;
        }

        public static bool AddScore(string songHash, string userId, string team, LevelDifficulty difficulty, PlayerOptions playerOptions, GameOptions gameOptions, bool fullCombo, long score)
        {
            return ExecuteCommand($"INSERT INTO scoreTable VALUES (NULL, \'{songHash}\', \'{userId}\', \'{team}\', {(int)difficulty}, {(int)playerOptions}, {(int)gameOptions}, {(fullCombo ? 1: 0)}, {score}, 0)") > 0;
        }

        public static bool AddSong(string songName, string songAuthor, string songSubtext, string songHash, LevelDifficulty difficulty, PlayerOptions playerOptions, GameOptions gameOptions)
        {
            return ExecuteCommand($"INSERT INTO songTable VALUES (NULL, \'{songName}\', \'{songAuthor}\', \'{songSubtext}\', \'{songHash}\', {(int)difficulty}, {(int)playerOptions}, {(int)gameOptions}, 0)") > 0;
        }

        public static bool AddTeam(string teamId, string teamName, string captainId, string color, int requiredTokens, string nextPromotion)
        {
            return ExecuteCommand($"INSERT INTO teamTable VALUES (NULL, \'{teamId}\', \'{teamName}\', \'{captainId}\', \'{color}\', {requiredTokens}, \'{nextPromotion}\', 0)") > 0;
        }

        public static bool AddVote(string userId, ulong messageId, string nextPromotion, int votes, int requiredVotes)
        {
            return ExecuteCommand($"INSERT INTO voteTable VALUES (NULL, \'{userId}\', {(long)messageId}, \'{nextPromotion}\', {votes}, {requiredVotes}, 0)") > 0;
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

        //Returns a list of all the votes
        public static List<Vote> GetAllVotes()
        {
            List<Vote> ret = new List<Vote>();
            SQLiteConnection db = OpenConnection();
            using (SQLiteCommand command = new SQLiteCommand($"SELECT userId FROM voteTable WHERE NOT old = 1", db))
            {
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        ret.Add(new Vote(reader["userId"].ToString(), "[deleted]"));
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
            using (SQLiteCommand command = new SQLiteCommand($"SELECT userId FROM playerTable WHERE NOT liquidated = 1", db))
            {
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        ret.Add(new Player(reader["userId"].ToString()));
                    }
                }
            }

            db.Close();

            return ret;
        }

        //Returns a list of SongConstruct of the currently active songs
        public static List<SongConstruct> GetActiveSongs(bool includeScores = false)
        {
            List<SongConstruct> ret = new List<SongConstruct>();
            SQLiteConnection db = OpenConnection();
            using (SQLiteCommand command = new SQLiteCommand("SELECT songHash, songName, difficulty, gameOptions, playerOptions FROM songTable WHERE NOT old = 1", db))
            {
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        SongConstruct item = new SongConstruct()
                        {
                            SongHash = reader["songHash"].ToString(),
                            Name = reader["songName"].ToString(),
                            Difficulty = (LevelDifficulty)Convert.ToInt32(reader["difficulty"].ToString()),
                            GameOptions = (GameOptions)Convert.ToInt32(reader["gameOptions"].ToString()),
                            PlayerOptions = (PlayerOptions)Convert.ToInt32(reader["playerOptions"].ToString())
                        };
                        ret.Add(item);
                    }
                }
            }

            db.Close();

            if (includeScores)
            {
                ret.ForEach(x =>
                {
                    if (x.Scores == null) x.Scores = GetScoresForSong(x, "-1");
                });
            }

            return ret;
        }

        //Returns a list of songs with score data intact
        public static List<SongConstruct> GetAllScores(string teamId = "-1")
        {
            List<SongConstruct> ret = GetActiveSongs();

            ret.ForEach(x =>
            {
                if (x.Scores == null) x.Scores = GetScoresForSong(x, teamId);
            });

            return ret;
        }

        //NOTE: If the song is on "auto" difficulty, it will return all difficulties
        public static List<ScoreConstruct> GetScoresForSong(SongConstruct s, string teamId = "-1")
        {
            List <ScoreConstruct> ret = new List<ScoreConstruct>();
            SQLiteConnection db = OpenConnection();
            using (SQLiteCommand command = new SQLiteCommand($"SELECT userId, score, fullCombo, difficulty, team FROM scoreTable WHERE songHash = \'{s.SongHash}\' {(s.Difficulty != LevelDifficulty.Auto ? $"AND difficulty = {(int)s.Difficulty}" : "")} {(teamId != "-1" ? $"AND team = \'{teamId}\'" : null)} AND NOT old = 1 ORDER BY score DESC", db))
            {
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        ret.Add(
                            new ScoreConstruct
                            {
                                UserId = reader["userId"].ToString(),
                                Score = Convert.ToInt32(reader["score"].ToString()),
                                FullCombo = reader["fullCombo"].ToString() == "True",
                                TeamId = reader["team"].ToString(),
                                Difficulty = (LevelDifficulty)Convert.ToInt64(reader["difficulty"].ToString())
                            });
                    }
                }
            }

            db.Close();

            return ret;
        }

        //Returns a dictionary of SongConstructs and scores for the designated player
        public static IDictionary<SongConstruct, ScoreConstruct> GetScoresForPlayer(string userId)
        {
            Dictionary<SongConstruct, ScoreConstruct> ret = new Dictionary<SongConstruct, ScoreConstruct>();
            SQLiteConnection db = OpenConnection();
            using (SQLiteCommand command = new SQLiteCommand($"SELECT score, songHash, team, difficulty, fullCombo, gameOptions, playerOptions FROM scoreTable WHERE userId = \'{userId}\' AND NOT old = 1", db))
            {
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        ret.Add(
                            new SongConstruct()
                            {
                                SongHash = reader["songHash"].ToString(),
                                GameOptions = (GameOptions)Convert.ToInt32(reader["gameOptions"].ToString()),
                                PlayerOptions = (PlayerOptions)Convert.ToInt32(reader["playerOptions"].ToString()),
                                Difficulty = (LevelDifficulty)Convert.ToInt32(reader["difficulty"].ToString())
                            },
                            new ScoreConstruct {
                                Score = Convert.ToInt32(reader["score"].ToString()),
                                FullCombo = reader["fullCombo"].ToString() == "True",
                                TeamId = reader["team"].ToString(),
                                Difficulty = (LevelDifficulty)Convert.ToInt64(reader["difficulty"].ToString())
                            });
                    }
                }
            }

            db.Close();

            return ret;
        }

        //<Player, tokens to be given>
        public static IDictionary<Player, int> GetTokenDispersal()
        {
            Dictionary<Player, int> playersToGiveTokens = new Dictionary<Player, int>();

            var teams = GetAllTeams();
            foreach (var team in teams)
            {
                GetAllScores(teamId: team.TeamId).ToList().ForEach(y =>
                {
                    if (y.Scores.Count > 0)
                    {
                        var player = new Player(y.Scores.ElementAt(0).UserId);
                        if (!playersToGiveTokens.ContainsKey(player)) playersToGiveTokens[player] = 0;
                        playersToGiveTokens[player] += 3;
                    }
                    if (y.Scores.Count > 1)
                    {
                        var player = new Player(y.Scores.ElementAt(1).UserId);
                        if (!playersToGiveTokens.ContainsKey(player)) playersToGiveTokens[player] = 0;
                        playersToGiveTokens[player] += 2;
                    }
                    if (y.Scores.Count > 2)
                    {
                        var player = new Player(y.Scores.ElementAt(2).UserId);
                        if (!playersToGiveTokens.ContainsKey(player)) playersToGiveTokens[player] = 0;
                        playersToGiveTokens[player] += 1;
                    }
                });
            }

            return playersToGiveTokens;
        }

        //Tiny classes to help organize leaderboard commands and reduce strain on SQL
        public class ScoreConstruct
        {
            public string UserId { get; set; }
            public int Score { get; set; }
            public bool FullCombo { get; set; }
            public string TeamId { get; set; }
            public LevelDifficulty Difficulty { get; set; }
        }

        public class SongConstruct
        {
            public string SongHash { get; set; }
            public string Name { get; set; }
            public LevelDifficulty Difficulty { get; set; }
            public GameOptions GameOptions { get; set; }
            public PlayerOptions PlayerOptions { get; set; }
            public List<ScoreConstruct> Scores { get; set; }

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
                hash = (hash * 7) + SongHash.GetHashCode();
                hash = (hash * 7) + Difficulty.GetHashCode();
                return hash;
            }
            //End necessary overrides
        }
    }
}
