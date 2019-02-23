using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static TeamSaberShared.SharedConstructs;

/*
 * Created by Moon on 9/11/2018
 * TODO: Use Properties (get/set) instead of getters and setters
 */

namespace TeamSaberServer.Database
{
    class Score
    {
        private Song song;
        private Player player;
        private LevelDifficulty _difficultyLevel;

        public Score(string songId, string steamId, LevelDifficulty levelDifficulty)
        {
            song = new Song(songId, levelDifficulty);
            player = new Player(steamId);
            _difficultyLevel = levelDifficulty;
            if (!Exists())
            {
                SimpleSql.AddScore(songId, steamId, player.GetRarity(), player.GetTeam(), levelDifficulty, false, 0);
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
            string scoreString = SimpleSql.ExecuteQuery($"SELECT score FROM scoreTable WHERE songId = \'{song.GetSongId()}\' AND steamId = {player.GetSteamId()}", "score").First();
            return Convert.ToInt64(scoreString);
        }

        public bool SetScore(long score, bool fullCombo)
        {
            return SimpleSql.ExecuteCommand($"UPDATE scoreTable SET score = {score}, fullCombo = {(fullCombo ? 1 : 0)}, old = 0 WHERE songId = \'{song.GetSongId()}\' AND steamId = {player.GetSteamId()}") > 1;
        }

        public bool SetOld()
        {
            return SimpleSql.ExecuteCommand($"UPDATE scoreTable SET old = 1 WHERE songId = \'{song.GetSongId()}\' AND difficultyLevel = {(int)_difficultyLevel} AND steamId = {player.GetSteamId()}") > 1;
        }

        public bool Exists()
        {
            return Exists(song.GetSongId(), player.GetSteamId(), _difficultyLevel);
        }

        public static bool Exists(string songId, string steamId, LevelDifficulty difficultyLevel)
        {
            return SimpleSql.ExecuteQuery($"SELECT * FROM scoreTable WHERE songId = \'{songId}\' AND steamId = {steamId} AND difficultyLevel = {(int)difficultyLevel} AND OLD = 0", "songId").Any();
        }
    }
}
