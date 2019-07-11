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

        public Score(string hash, string userId, LevelDifficulty difficulty)
        {
            song = new Song(hash, difficulty);
            player = new Player(userId);
            this.difficulty = difficulty;
            if (!Exists())
            {
                SqlUtils.AddScore(hash, userId, player.Team, difficulty, PlayerOptions.None, GameOptions.None, false, 0);
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
            string scoreString = SqlUtils.ExecuteQuery($"SELECT score FROM scoreTable WHERE songHash = \'{song.Hash}\' AND difficulty = {(int)difficulty} AND userId = {player.UserId} AND old = 0", "score").First();
            return Convert.ToInt64(scoreString);
        }

        public bool SetScore(long score, bool fullCombo)
        {
            return SqlUtils.ExecuteCommand($"UPDATE scoreTable SET score = {score}, fullCombo = {(fullCombo ? 1 : 0)} WHERE songHash = \'{song.Hash}\' AND difficulty = {(int)difficulty} AND userId = {player.UserId} AND old = 0") > 1;
        }

        public bool SetOld()
        {
            return SqlUtils.ExecuteCommand($"UPDATE scoreTable SET old = 1 WHERE songHash = \'{song.Hash}\' AND difficulty = {(int)difficulty} AND userId = {player.UserId}") > 1;
        }

        public bool Exists()
        {
            return Exists(song.Hash, player.UserId, difficulty);
        }

        public static bool Exists(string songHash, string userId, LevelDifficulty difficulty)
        {
            return SqlUtils.ExecuteQuery($"SELECT * FROM scoreTable WHERE songHash = \'{songHash}\' AND userId = \'{userId}\' AND difficulty = {(int)difficulty} AND old = 0", "songHash").Any();
        }

        //KotH Event-specific
        //Deletes scores on other songs
        public bool DeleteOtherScoresForUser() => DeleteOtherScoresForUser(song.Hash, player.UserId);
        public static bool DeleteOtherScoresForUser(string songHash, string userId)
        {
            return SqlUtils.ExecuteCommand($"UPDATE scoreTable SET old = 1 WHERE NOT songHash = \'{songHash}\' AND userId = \'{userId}\'") > 1;
        }
    }
}
