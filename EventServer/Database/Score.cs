using System;
using System.Linq;
using static EventShared.SharedConstructs;

/*
 * Created by Moon on 9/11/2018
 * TODO: Use Properties (get/set) instead of getters and setters
 */

namespace EventServer.Database
{
    class Score
    {
        private Song song;
        private Player player;
        private LevelDifficulty difficulty;

        public Score(string songId, string steamId, LevelDifficulty difficulty)
        {
            song = new Song(songId, difficulty);
            player = new Player(steamId);
            this.difficulty = difficulty;
            if (!Exists())
            {
                SimpleSql.AddScore(songId, steamId, player.Team, difficulty, PlayerOptions.None, GameOptions.None, false, 0);
            }
        }

        public Song GetSong()
        {
            return song;
        }

        public Player GetPlayer()
        {
            return player;
        }

        public long GetScore()
        {
            string scoreString = SimpleSql.ExecuteQuery($"SELECT score FROM scoreTable WHERE songId = \'{song.SongId}\' AND difficulty = {(int)difficulty} AND steamId = {player.SteamId} AND old = 0", "score").First();
            return Convert.ToInt64(scoreString);
        }

        public bool SetScore(long score, bool fullCombo)
        {
            return SimpleSql.ExecuteCommand($"UPDATE scoreTable SET score = {score}, fullCombo = {(fullCombo ? 1 : 0)} WHERE songId = \'{song.SongId}\' AND difficulty = {(int)difficulty} AND steamId = {player.SteamId} AND old = 0") > 1;
        }

        public bool SetOld()
        {
            return SimpleSql.ExecuteCommand($"UPDATE scoreTable SET old = 1 WHERE songId = \'{song.SongId}\' AND difficulty = {(int)difficulty} AND steamId = {player.SteamId}") > 1;
        }

        public bool Exists()
        {
            return Exists(song.SongId, player.SteamId, difficulty);
        }

        public static bool Exists(string songId, string steamId, LevelDifficulty difficulty)
        {
            return SimpleSql.ExecuteQuery($"SELECT * FROM scoreTable WHERE songId = \'{songId}\' AND steamId = {steamId} AND difficulty = {(int)difficulty} AND old = 0", "songId").Any();
        }

        //KotH Event-specific
        //Deletes scores on other songs
        public bool DeleteOtherScoresForUser() => DeleteOtherScoresForUser(song.SongId, player.SteamId);
        public static bool DeleteOtherScoresForUser(string songId, string steamId)
        {
            return SimpleSql.ExecuteCommand($"UPDATE scoreTable SET old = 1 WHERE NOT songId = \'{songId}\' AND steamId = {steamId}") > 1;
        }
    }
}
