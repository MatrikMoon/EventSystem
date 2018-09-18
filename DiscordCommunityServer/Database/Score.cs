using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/*
 * Created by Moon on 9/11/2018
 * TODO: Use Properties (get/set) instead of getters and setters
 */

namespace DiscordCommunityServer.Database
{
    class Score
    {
        private Song song;
        private Player player;
        private int _difficultyLevel;

        public Score(string songId, string steamId, int difficultyLevel, int gameplayMode)
        {
            song = new Song(songId, gameplayMode);
            player = new Player(steamId);
            _difficultyLevel = difficultyLevel;
            if (!Exists())
            {
                SimpleSql.AddScore(songId, steamId, player.GetRank(), difficultyLevel, gameplayMode, 0);
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

        public bool SetScore(long score)
        {
            return SimpleSql.ExecuteCommand($"UPDATE scoreTable SET score = {score} WHERE songId = \'{song.GetSongId()}\' AND steamId = {player.GetSteamId()}") > 1;
        }

        public bool Exists()
        {
            return Exists(song.GetSongId(), player.GetSteamId(), _difficultyLevel);
        }

        public static bool Exists(string songId, string steamId, int difficultyLevel)
        {
            return SimpleSql.ExecuteQuery($"SELECT * FROM scoreTable WHERE songId = \'{songId}\' AND steamId = {steamId} AND difficultyLevel = {difficultyLevel}", "songId").Any();
        }
    }
}
