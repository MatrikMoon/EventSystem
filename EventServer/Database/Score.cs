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
        private string characteristic;

        public Score(string hash, string userId, LevelDifficulty difficulty, string characteristic)
        {
            song = new Song(hash, difficulty, characteristic);
            player = new Player(userId);
            this.difficulty = difficulty;
            this.characteristic = characteristic;
            if (!Exists())
            {
                SqlUtils.AddScore(hash, userId, player.Team, difficulty, characteristic, PlayerOptions.None, GameOptions.None, false, 0);
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
            string scoreString = SqlUtils.ExecuteQuery($"SELECT score FROM scoreTable WHERE songHash = \'{song.Hash}\' AND difficulty = {(int)difficulty} AND characteristic = \'{characteristic}\' AND userId = {player.UserId} AND old = 0", "score").First();
            return Convert.ToInt64(scoreString);
        }

        public bool SetScore(long score, bool fullCombo)
        {
            return SqlUtils.ExecuteCommand($"UPDATE scoreTable SET score = {score}, fullCombo = {(fullCombo ? 1 : 0)} WHERE songHash = \'{song.Hash}\' AND difficulty = {(int)difficulty} AND characteristic = \'{characteristic}\' AND userId = {player.UserId} AND old = 0") > 1;
        }

        public bool SetOld()
        {
            return SqlUtils.ExecuteCommand($"UPDATE scoreTable SET old = 1 WHERE songHash = \'{song.Hash}\' AND difficulty = {(int)difficulty} AND characteristic = \'{characteristic}\' AND userId = {player.UserId}") > 1;
        }

        public bool Exists()
        {
            return Exists(song.Hash, player.UserId, difficulty, characteristic);
        }

        public static bool Exists(string songHash, string userId, LevelDifficulty difficulty, string characteristic)
        {
            return SqlUtils.ExecuteQuery($"SELECT * FROM scoreTable WHERE songHash = \'{songHash}\' AND userId = \'{userId}\' AND difficulty = {(int)difficulty} AND characteristic = \'{characteristic}\' AND old = 0", "songHash").Any();
        }
    }
}
