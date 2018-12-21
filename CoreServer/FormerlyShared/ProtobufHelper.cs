using Google.Protobuf;
using System;

namespace ChristmasShared
{
    public class ProtobufHelper
    {
        public static byte[] SerializeProtobuf(object proto)
        {
            if (proto is Vote)
            {
                return ((IMessage)proto).ToByteArray();
            }
            throw new Exception("proto is not a Protobuf object");
        }
    }
}
