using Google.Protobuf;
using System;

namespace EventShared
{
    public class ProtobufHelper
    {
        public static byte[] SerializeProtobuf(object proto)
        {
            if (proto is Score || proto is Sabotage)
            {
                return ((IMessage)proto).ToByteArray();
            }
            throw new Exception("proto is not a Protobuf object");
        }
    }
}
