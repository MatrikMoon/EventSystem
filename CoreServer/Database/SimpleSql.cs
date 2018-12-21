using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.IO;
using ChristmasShared;
using static ChristmasShared.SharedConstructs;

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
        private static string databaseName = "VoteDatabase";
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

                ExecuteCommand("CREATE TABLE IF NOT EXISTS playerTable (_id INTEGER PRIMARY KEY AUTOINCREMENT, steamId TEXT DEFAULT '', discordName TEXT DEFAULT '', discordExtension TEXT DEFAULT '', discordMention TEXT DEFAULT '', liquidated BIT DEFAULT 0)");
                ExecuteCommand("CREATE TABLE IF NOT EXISTS itemTable (_id INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT DEFAULT '', author TEXT DEFAULT '', subName TEXT DEFAULT '', itemId TEXT DEFAULT '', category TEXT DEFAULT '', old BIT DEFAULT 0)");
                ExecuteCommand("CREATE TABLE IF NOT EXISTS voteTable (_id INTEGER PRIMARY KEY AUTOINCREMENT, userId TEXT DEFAULT '', itemId TEXT DEFAULT '', category TEXT DEFAULT '', old BIT DEFAULT 0)");
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

        public static bool AddItem(string name, string author, string subName, string itemId, Category category)
        {
            return ExecuteCommand($"INSERT INTO itemTable VALUES (NULL, \'{name}\', \'{author}\', \'{subName}\', \'{itemId}\', \'{(int)category}\', 0)") > 0;
        }

        public static bool AddVote(string userId, string itemId, Category category)
        {
            return ExecuteCommand($"INSERT INTO voteTable VALUES (NULL, \'{userId}\', \'{itemId}\', \'{(int)category}\', 0)") > 0;
        }

        //Returns a list of SongConstruct of the currently active songs
        public static List<Item> GetActiveItems(Category category)
        {
            List<Item> ret = new List<Item>();
            SQLiteConnection db = OpenConnection();
            using (SQLiteCommand command = new SQLiteCommand("SELECT itemId, category FROM itemTable WHERE NOT old = 1", db))
            {
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        ret.Add(
                            new Item(
                                reader["itemId"].ToString(),
                                (Category)Convert.ToInt32(reader["category"].ToString())
                            )
                        );
                    }
                }
            }

            db.Close();

            return ret;
        }

        //Returns a list of SongConstruct of the currently active songs
        public static List<Item> GetVotesForPlayer(string userId)
        {
            List<Item> ret = new List<Item>();
            SQLiteConnection db = OpenConnection();
            using (SQLiteCommand command = new SQLiteCommand($"SELECT itemId, category FROM voteTable WHERE userId = \'{userId}\' AND NOT old = 1", db))
            {
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        ret.Add(
                            new Item(
                                reader["itemId"].ToString(),
                                (Category)Convert.ToInt32(reader["category"].ToString())
                            )
                        );
                    }
                }
            }

            db.Close();

            return ret;
        }
    }
}
