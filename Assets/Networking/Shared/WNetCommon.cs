using Networking;
using System;
using UnityEngine;

namespace Networking.Shared
{
    public enum WConnectionType {
        TCP,
        UDP
    }


    public class WNetCommon {
        public const int MAX_NETMESSAGE_PACKET_SIZE = 4096;
        public const int TICKS_PER_UPDATE = 5;
        public const ushort WRONGWARP_PORT = 1972;
    }
}
