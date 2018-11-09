using Google.Protobuf;
using System;

namespace DiscordCommunityShared
{
    public class ProtobufHelper
    {
        public static byte[] SerializeProtobuf(object proto)
        {
            if (proto is Score || proto is RankRequest)
            {
                return ((IMessage)proto).ToByteArray();
            }
            throw new Exception("proto is not a Protobuf object");
        }
    }
}
