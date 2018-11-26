using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.IO;
using DiscordCommunityShared;
using static DiscordCommunityShared.SharedConstructs;

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

        //Returns a list of SongConstruct of the currently active songs
        public static List<SongConstruct> GetActiveSongs()
        {
            List<SongConstruct> ret = new List<SongConstruct>();
            SQLiteConnection db = OpenConnection();
            using (SQLiteCommand command = new SQLiteCommand("SELECT songId, mode, songName FROM songTable WHERE NOT old = 1", db))
            {
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        SongConstruct item = new SongConstruct()
                        {
                            SongId = reader["songId"].ToString(),
                            Mode = reader["mode"].ToString(),
                            Name = reader["songName"].ToString()
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
        public static IDictionary<SongConstruct, IDictionary<string, ScoreConstruct>> GetAllActiveScoresForRank(SharedConstructs.Rank r)
        {
            List<SongConstruct> songs = GetActiveSongs();

            Dictionary<SongConstruct, IDictionary<string, ScoreConstruct>> scores = new Dictionary<SongConstruct, IDictionary<string, ScoreConstruct>>();
            songs.ForEach(x => {
                string songId = x.SongId;
                int mode = Convert.ToInt32(x.Mode);
                scores.Add(x, GetScoresForSong(x, (long)r));
            });

            return scores;
        }

        //Returns a dictionary of steamIds and scores for the designated song and rank
        public static IDictionary<string, ScoreConstruct> GetScoresForSong(SongConstruct s, long rank)
        {
            Dictionary<string, ScoreConstruct> ret = new Dictionary<string, ScoreConstruct>();
            SQLiteConnection db = OpenConnection();
            using (SQLiteCommand command = new SQLiteCommand($"SELECT steamId, score, fullCombo, difficultyLevel, rank FROM scoreTable WHERE songId = \'{s.SongId}\' {((Rank)rank != Rank.All ? $"AND rank = \'{rank}\'" : null)} AND mode = {s.Mode} AND NOT old = 1 ORDER BY score DESC", db))
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
                                Rank = (Rank)Convert.ToInt64(reader["rank"].ToString()),
                                Difficulty = (LevelDifficulty)Convert.ToInt64(reader["difficultyLevel"].ToString())
                            });
                    }
                }
            }

            db.Close();

            return ret;
        }

        //Returns a dictionary of SongConstructs and scores for the designated song and rank
        public static IDictionary<SongConstruct, ScoreConstruct> GetScoresForPlayer(string steamId)
        {
            Dictionary<SongConstruct, ScoreConstruct> ret = new Dictionary<SongConstruct, ScoreConstruct>();
            SQLiteConnection db = OpenConnection();
            using (SQLiteCommand command = new SQLiteCommand($"SELECT score, mode, songId, rank, difficultyLevel, fullCombo FROM scoreTable WHERE steamId = \'{steamId}\' AND NOT old = 1", db))
            {
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        ret.Add(
                            new SongConstruct()
                            {
                                SongId = reader["songId"].ToString(),
                                Mode = reader["mode"].ToString()
                            },
                            new ScoreConstruct {
                                Score = Convert.ToInt64(reader["score"].ToString()),
                                FullCombo = reader["fullCombo"].ToString() == "True",
                                Rank = (Rank)Convert.ToInt64(reader["rank"].ToString()),
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
            public Rank Rank { get; set; }
            public LevelDifficulty Difficulty { get; set; }
        }

        public class SongConstruct
        {
            public string SongId { get; set; }
            public string Mode { get; set; }
            public string Name { get; set; }

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
                hash = (hash * 7) + Mode.GetHashCode();
                return hash;
            }
            //End necessary overrides
        }
    }
}
