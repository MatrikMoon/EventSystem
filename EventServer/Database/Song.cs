using EventShared;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using static EventShared.SharedConstructs;

/*
 * Created by Moon on 9/11/2018
 * TODO: Formal identification for songs, not reliant upon some (but not other) properties of the song
 */

namespace EventServer.Database
{
    class Song
    {
        public string Hash { get; private set; }
        public string SongName
        {
            get => SqlUtils.ExecuteQuery($"SELECT songName FROM songTable WHERE songHash = \'{Hash}\' AND difficulty = {(int)Difficulty} AND characteristic = \'{Characteristic}\'", "songName").First();
            set => SqlUtils.ExecuteCommand($"UPDATE songTable SET songName = \'{value}\' WHERE songHash = \'{Hash}\' AND difficulty = {(int)Difficulty} AND characteristic = \'{Characteristic}\'");
        }
        public int GameOptions
        {
            get
            {
                var optionString = SqlUtils.ExecuteQuery($"SELECT gameOptions FROM songTable WHERE songHash = \'{Hash}\' AND difficulty = {(int)Difficulty} AND characteristic = \'{Characteristic}\'", "gameOptions").First();
                return Convert.ToInt32(optionString);
            }
            set => SqlUtils.ExecuteCommand($"UPDATE songTable SET gameOptions = {value} WHERE songHash = \'{Hash}\' AND difficulty = {(int)Difficulty} AND characteristic = \'{Characteristic}\'");
        }
        public int PlayerOptions
        {
            get
            {
                var optionString = SqlUtils.ExecuteQuery($"SELECT playerOptions FROM songTable WHERE songHash = \'{Hash}\' AND difficulty = {(int)Difficulty} AND characteristic = \'{Characteristic}\'", "playerOptions").First();
                return Convert.ToInt32(optionString);
            }
            set => SqlUtils.ExecuteCommand($"UPDATE songTable SET playerOptions = {value} WHERE songHash = \'{Hash}\' AND difficulty = {(int)Difficulty} AND characteristic = \'{Characteristic}\'");
        }
        public bool Old
        {
            get => SqlUtils.ExecuteQuery($"SELECT old FROM songTable WHERE songHash = \'{Hash}\' AND difficulty = {(int)Difficulty} AND characteristic = \'{Characteristic}\'", "old").First() == "1";
            set => SqlUtils.ExecuteCommand($"UPDATE songTable SET old = \'{(value ? "1" : "0")}\' WHERE songHash = \'{Hash}\' AND difficulty = {(int)Difficulty} AND characteristic = \'{Characteristic}\'");
        }
        public LevelDifficulty Difficulty { get; private set; }
        public string Characteristic { get; private set; }

        public Song(string hash, LevelDifficulty difficulty, string characteristic)
        {
            Difficulty = difficulty;
            Characteristic = characteristic;
            Hash = hash;
            if (!Exists(true))
            {
                //Add a placeholder, trigger song download from BeatSaver if it doesn't exist
                SqlUtils.AddSong("", "", "", hash, difficulty, characteristic, SharedConstructs.PlayerOptions.None, SharedConstructs.GameOptions.None);
                if (OstHelper.IsOst(hash))
                {
                    string songName = OstHelper.GetOstSongNameFromLevelId(hash);
                    SongName = Regex.Replace(songName, "[^a-zA-Z0-9- ]", "");
                }
                else BeatSaver.BeatSaverDownloader.DownloadSongInfoThreaded(Hash, (b) =>
                {
                    if (b)
                    {
                        string songName = new BeatSaver.Song(Hash).SongName;
                        SongName = Regex.Replace(songName, "[^a-zA-Z0-9- ]", "");
                    }
                    else SongName = "[Could not download song info]";
                });
            }
            else if (ExistsAsAutoDifficulty()) Difficulty = LevelDifficulty.Auto;
        }

        public bool Exists(bool allowAutoDifficulty = false)
        {
            return Exists(Hash, Difficulty, Characteristic, allowAutoDifficulty);
        }

        public static bool Exists(string songHash, LevelDifficulty difficulty, string characteristic, bool allowAutoDifficulty = false)
        {
            return SqlUtils.ExecuteQuery($"SELECT * FROM songTable WHERE songHash = \'{songHash}\' AND characteristic = \'{characteristic}\' AND (difficulty = {(int)difficulty}{(allowAutoDifficulty ? " OR difficulty = -1)" : ")")} AND old = 0", "songHash").Any();
        }

        public bool ExistsAsAutoDifficulty() => ExistsAsAutoDifficulty(Hash);
        public static bool ExistsAsAutoDifficulty(string hash)
        {
            return SqlUtils.ExecuteQuery($"SELECT * FROM songTable WHERE songHash = \'{hash}\' AND difficulty = -1 AND old = 0", "songHash").Any();
        }
    }
}
