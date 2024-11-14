using Networking;
using System;
using UnityEngine;

namespace Code.Shared
{
    public enum WConnectionType {
        TCP,
        UDP
    }


    public class WNetCommon {
        public const int MAX_NETMESSAGE_PACKET_SIZE = 4096;
        public const ushort WRONGWARP_PORT = 1972;
    }
}
