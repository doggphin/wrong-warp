using Networking;
using System;
using UnityEngine;

namespace Networking
{
    public enum ConnectionType {
        TCP,
        UDP
    }


    public class NetCommon
    {
        public const int MAX_NETMESSAGE_PACKET_SIZE = 4096;

        public static Vector3 DeserializeVector3(byte[] data, int readFrom) {
            return new Vector3(
                BitConverter.ToSingle(data, readFrom),
                BitConverter.ToSingle(data, readFrom + 4),
                BitConverter.ToSingle(data, readFrom + 8)
            );
        }


        public static void SerializeVector3(byte[] data, Vector3 vec, int writeAt) {
            Buffer.BlockCopy(BitConverter.GetBytes(vec.x), 0, data, writeAt, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(vec.y), 0, data, writeAt + 4, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(vec.z), 0, data, writeAt + 8, 4);
        }
    }
}
