using System.Collections.Generic;
using System.Linq;
using System;

namespace Networking
{
    public class NetPacket
    {
        public ulong tick;
        public List<byte> msg;
        private int currentDataLength;

        public NetPacket(ulong tick)
        {
            this.tick = tick;
            msg = BitConverter.GetBytes(tick).ToList();
            currentDataLength = 4;
        }

        public bool AppendToMsg(byte[] data)
        {
            if (currentDataLength + data.Length > NetCommon.MAX_NETMESSAGE_PACKET_SIZE)
            {
                return false;
            }

            currentDataLength += data.Length;
            msg.AddRange(data);

            return true;
        }
    }
}
