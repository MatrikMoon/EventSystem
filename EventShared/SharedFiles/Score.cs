using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

/**
 * Created by Moon on 4/11/2019
 * Serializable score class
 */

namespace EventShared
{
    [Obfuscation(Exclude = false, Feature = "+rename(mode=decodable,renPdb=true)")]
    class Score
    {
        public string UserId { get; private set; }
        public string SongId { get; private set; }
        public int Score_ { get; private set; }
        public int Difficulty { get; private set; }
        public bool FullCombo { get; private set; }
        public int PlayerOptions { get; private set; }
        public int GameOptions { get; private set; }
        public string Signed { get; private set; }

        public Score(
            string userId,
            string songId,
            int score,
            int difficulty,
            bool fullCombo,
            int playerOptions,
            int gameOptions,
            string signed)
        {
            UserId = userId;
            SongId = songId;
            Score_ = score;
            Difficulty = difficulty;
            FullCombo = fullCombo;
            PlayerOptions = playerOptions;
            GameOptions = gameOptions;
            Signed = signed;
        }

        public static Score FromString(string base64)
        {
            var stream = new MemoryStream(Convert.FromBase64String(base64));

            var magicFlagBytes = new byte[sizeof(byte) * 4];
            var userIdBytes = new List<byte>();
            var songIdBytes = new List<byte>();
            var scoreBytes = new byte[sizeof(int)];
            var difficultyBytes = new byte[sizeof(int)];
            var fullComboBytes = new byte[sizeof(bool)];
            var playerOptionsBytes = new byte[sizeof(int)];
            var gameOptionsBytes = new byte[sizeof(int)];
            var signedBytes = new List<byte>();

            //Verify that this file was indeed made by us
            stream.Read(magicFlagBytes, 0, sizeof(byte) * 4);
            if (Encoding.UTF8.GetString(magicFlagBytes) != "moon") throw new FormatException();

            //Is there a prebuilt thing to do this?
            byte read = (byte)stream.ReadByte();
            while (read != 0x0)
            {
                userIdBytes.Add(read);
                read = (byte)stream.ReadByte();
            }

            read = (byte)stream.ReadByte();
            while (read != 0x0)
            {
                songIdBytes.Add(read);
                read = (byte)stream.ReadByte();
            }

            stream.Read(scoreBytes, 0, sizeof(int));
            stream.Read(difficultyBytes, 0, sizeof(int));
            stream.Read(fullComboBytes, 0, sizeof(bool));
            stream.Read(playerOptionsBytes, 0, sizeof(int));
            stream.Read(gameOptionsBytes, 0, sizeof(int));

            read = (byte)stream.ReadByte();
            while (read != 0x0)
            {
                signedBytes.Add(read);
                read = (byte)stream.ReadByte();
            }

            var songId = Encoding.UTF8.GetString(songIdBytes.ToArray());
            var userId = Encoding.UTF8.GetString(userIdBytes.ToArray());
            var score = BitConverter.ToInt32(scoreBytes, 0);
            var difficulty = BitConverter.ToInt32(difficultyBytes, 0);
            var fullCombo = BitConverter.ToBoolean(fullComboBytes, 0);
            var playerOptions = BitConverter.ToInt32(playerOptionsBytes, 0);
            var gameOptions = BitConverter.ToInt32(gameOptionsBytes, 0);
            var signed = Encoding.UTF8.GetString(signedBytes.ToArray());

            return new Score(userId, songId, score, difficulty, fullCombo, playerOptions, gameOptions, signed);
        }

        public string ToBase64()
        {
            var magicFlag = Encoding.UTF8.GetBytes("moon");
            var userIdBytes = Combine(new byte[][] { Encoding.UTF8.GetBytes(UserId), new byte[] { 0x0 } });
            var songIdBytes = Combine(new byte[][] { Encoding.UTF8.GetBytes(SongId), new byte[] { 0x0 } });
            var scoreBytes = BitConverter.GetBytes(Score_);
            var difficultyBytes = BitConverter.GetBytes(Difficulty);
            var fullComboBytes = BitConverter.GetBytes(FullCombo);
            var playerOptionsBytes = BitConverter.GetBytes(PlayerOptions);
            var gameOptionsBytes = BitConverter.GetBytes(GameOptions);
            var signedBytes = Combine(new byte[][] { Encoding.UTF8.GetBytes(Signed), new byte[] { 0x0 } });

            var allBytes = Combine(new byte[][] { magicFlag, userIdBytes, songIdBytes, scoreBytes, difficultyBytes, fullComboBytes, playerOptionsBytes, gameOptionsBytes, signedBytes });
            return Convert.ToBase64String(allBytes);
        }

        private static byte[] Combine(params byte[][] arrays)
        {
            byte[] rv = new byte[arrays.Sum(a => a.Length)];
            int offset = 0;
            foreach (byte[] array in arrays)
            {
                Buffer.BlockCopy(array, 0, rv, offset, array.Length);
                offset += array.Length;
            }
            return rv;
        }
    }
}
