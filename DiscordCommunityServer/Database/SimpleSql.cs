using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.IO;
using DiscordCommunityShared;

/*
 * Created by Moon on 9/11/2018
 * Helper, assisting in interacting with the database
 * TODO: Use Linq for SQL
 */

namespace DiscordCommunityServer.Database
{
    public class SimpleSql
    {
        private static string databaseLocation = Directory.GetCurrentDirectory();
        private static string databaseName = "DiscordCommunityDatabase";
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

                ExecuteCommand("CREATE TABLE IF NOT EXISTS playerTable (_id INTEGER PRIMARY KEY AUTOINCREMENT, steamId TEXT DEFAULT '', discordName TEXT DEFAULT '', discordExtension TEXT DEFAULT '', discordMention TEXT DEFAULT '', rank INTEGER DEFAULT 0, tokens INTEGER DEFAULT 0, totalScore BIGINT DEFAULT 0, topScores BIGINT DEFAULT 0, songsPlayed INTEGER DEFAULT 0, personalBestsBeaten INTEGER DEFAULT 0, playersBeat INTEGER DEFAULT 0, mentionMe BIT DEFAULT 0, liquidated BIT DEFAULT 0)");
                ExecuteCommand("CREATE TABLE IF NOT EXISTS scoreTable (_id INTEGER PRIMARY KEY AUTOINCREMENT, songId TEXT DEFAULT '', steamId TEXT DEFAULT '', rank INTEGER DEFAULT 0, difficultyLevel INTEGER DEFAULT 0, mode INTEGER DEFAULT 0, fullCombo BIT DEFAULT 0, score BIGINT DEFAULT 0, old BIT DEFAULT 0)");
                ExecuteCommand("CREATE TABLE IF NOT EXISTS songTable (_id INTEGER PRIMARY KEY AUTOINCREMENT, songName TEXT DEFAULT '', songAuthor TEXT DEFAULT '', songSubtext TEXT DEFAULT '', songId TEXT DEFAULT '', mode INTEGER DEFAULT 0, old BIT DEFAULT 0)");
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

        public static List<Dictionary<string, string>> ExecuteQuery(string query, params string[] columnsToReturn)
        {
            List<Dictionary<string, string>> ret = new List<Dictionary<string, string>>();
            SQLiteConnection db = OpenConnection();
            using (SQLiteCommand command = new SQLiteCommand(query, db))
            {
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Dictionary<string, string> item = new Dictionary<string, string>();
                        columnsToReturn.ToList().ForEach(x =>
                        {
                            item.Add(x, reader[x]?.ToString());
                        });
                        ret.Add(item);
                    }
                }
            }

            db.Close();

            return ret;
        }

        public static bool AddPlayer(string steamId, string discordName, string discordExtension, string discordMention, int rank, int tokens, long totalScore, long topScores, int songsPlayed, int personalBestsBeaten, int playersBeat, bool mentionMe)
        {
            return ExecuteCommand($"INSERT INTO playerTable VALUES (NULL, \'{steamId}\', \'{discordName}\', \'{discordExtension}\', \'{discordMention}\', {rank}, {tokens}, {totalScore}, {topScores}, {songsPlayed}, {personalBestsBeaten}, {playersBeat}, {(mentionMe ? "1" : "0")}, 0)") > 0;
        }

        public static bool AddScore(string songId, string steamId, int rank, int difficultyLevel, int mode, bool fullCombo, long score)
        {
            return ExecuteCommand($"INSERT INTO scoreTable VALUES (NULL, \'{songId}\', \'{steamId}\', {rank}, {difficultyLevel}, {mode}, {(fullCombo ? 1: 0)}, {score}, 0)") > 0;
        }

        public static bool AddSong(string songName, string songAuthor, string songSubtext, string songId, int mode)
        {
            return ExecuteCommand($"INSERT INTO songTable VALUES (NULL, \'{songName}\', \'{songAuthor}\', \'{songSubtext}\', \'{songId}\', \'{mode}\', 0)") > 0;
        }

        //This marks all songs and scores as "old"
        public static int MarkAllOld()
        {
            int ret = 0;
            ret += ExecuteCommand("UPDATE songTable SET old = 1");
            ret += ExecuteCommand("UPDATE scoreTable SET old = 1");
            return ret;
        }

        //These are in here and not the Scores.cs file because in here, we have direct access to the database and
        //can grab what we need in less queries
        //Returns a dictionary of <songId + mode, Dictionary<steamId, ScoreConstruct>>
        public static IDictionary<string, IDictionary<string, ScoreConstruct>> GetAllActiveScoresForRank(SharedConstructs.Rank r)
        {
            List<Dictionary<string, string>> songIds = ExecuteQuery("SELECT songId, mode FROM songTable WHERE NOT old = 1", "songId", "mode");
            Dictionary<string, IDictionary<string, ScoreConstruct>> scores = new Dictionary<string, IDictionary<string, ScoreConstruct>>();
            songIds.ForEach(x => {
                string songId = x["songId"];
                int mode = Convert.ToInt32(x["mode"]);
                scores.Add(songId + mode, GetScoresForSong(songId, mode, (long)r));
            });

            return scores;
        }

        //Returns a dictionary of steamIds and scores for the designated song and rank
        public static IDictionary<string, ScoreConstruct> GetScoresForSong(string songId, int mode, long rank)
        {
            Dictionary<string, ScoreConstruct> ret = new Dictionary<string, ScoreConstruct>();
            SQLiteConnection db = OpenConnection();
            using (SQLiteCommand command = new SQLiteCommand($"SELECT steamId, score, fullCombo FROM scoreTable WHERE songId = \'{songId}\' AND rank = \'{rank}\' AND mode = {mode} AND NOT old = 1 ORDER BY score DESC", db))
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
                                FullCombo = reader["fullCombo"].ToString() == "True"
                            });
                    }
                }
            }

            db.Close();

            return ret;
        }

        //Returns a dictionary of songIds + mode and scores for the designated song and rank
        public static IDictionary<string, ScoreConstruct> GetScoresForPlayer(string steamId)
        {
            Dictionary<string, ScoreConstruct> ret = new Dictionary<string, ScoreConstruct>();
            SQLiteConnection db = OpenConnection();
            using (SQLiteCommand command = new SQLiteCommand($"SELECT score, mode, songId, fullCombo FROM scoreTable WHERE steamId = \'{steamId}\' AND NOT old = 1", db))
            {
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        ret.Add(
                            reader["songId"].ToString() + reader["mode"].ToString(), 
                            new ScoreConstruct {
                                Score = Convert.ToInt64(reader["score"].ToString()),
                                FullCombo = reader["fullCombo"].ToString() == "True"
                            });
                    }
                }
            }

            db.Close();

            return ret;
        }

        //Tiny class to help organize leaderboard commands
        public class ScoreConstruct
        {
            public long Score { get; set; }
            public bool FullCombo { get; set; }
        }
    }
}
