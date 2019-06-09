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
    class RankRequest
    {
        public string UserId { get; private set; }
        public string RequestedTeamId { get; private set; }
        public string OstScoreInfo { get; private set; }
        public bool InitialAssignment { get; private set; }
        public string Signed { get; private set; }

        public RankRequest(
            string userId,
            string requestedTeamId,
            string ostScoreInfo,
            bool initialRequest,
            string signed)
        {
            UserId = userId;
            RequestedTeamId = requestedTeamId;
            OstScoreInfo = ostScoreInfo;
            InitialAssignment = initialRequest;
            Signed = signed;
        }

        public static RankRequest FromString(string base64)
        {
            var stream = new MemoryStream(Convert.FromBase64String(base64));

            var magicFlagBytes = new byte[sizeof(byte) * 4];
            var userIdBytes = new List<byte>();
            var requestedTeamIdBytes = new List<byte>();
            var ostScoreInfoBytes = new List<byte>();
            var initialAssignmentBytes = new byte[sizeof(bool)];
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
                requestedTeamIdBytes.Add(read);
                read = (byte)stream.ReadByte();
            }

            read = (byte)stream.ReadByte();
            while (read != 0x0)
            {
                ostScoreInfoBytes.Add(read);
                read = (byte)stream.ReadByte();
            }

            stream.Read(initialAssignmentBytes, 0, sizeof(bool));

            read = (byte)stream.ReadByte();
            while (read != 0x0)
            {
                signedBytes.Add(read);
                read = (byte)stream.ReadByte();
            }

            var userId = Encoding.UTF8.GetString(userIdBytes.ToArray());
            var requestedTeamId = Encoding.UTF8.GetString(requestedTeamIdBytes.ToArray());
            var ostScoreInfo = Encoding.UTF8.GetString(ostScoreInfoBytes.ToArray());
            var initialAssignment = BitConverter.ToBoolean(initialAssignmentBytes, 0);
            var signed = Encoding.UTF8.GetString(signedBytes.ToArray());

            return new RankRequest(userId, requestedTeamId, ostScoreInfo, initialAssignment, signed);
        }

        public string ToBase64()
        {
            var magicFlag = Encoding.UTF8.GetBytes("moon");
            var userIdBytes = Combine(new byte[][] { Encoding.UTF8.GetBytes(UserId), new byte[] { 0x0 } });
            var requestedTeamIdBytes = Combine(new byte[][] { Encoding.UTF8.GetBytes(RequestedTeamId), new byte[] { 0x0 } });
            var ostScoreInfoBytes = Combine(new byte[][] { Encoding.UTF8.GetBytes(OstScoreInfo), new byte[] { 0x0 } });
            var initialAssignmentBytes = BitConverter.GetBytes(InitialAssignment);
            var signedBytes = Combine(new byte[][] { Encoding.UTF8.GetBytes(Signed), new byte[] { 0x0 } });

            var allBytes = Combine(new byte[][] { magicFlag, userIdBytes, requestedTeamIdBytes, ostScoreInfoBytes, initialAssignmentBytes, signedBytes });
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
