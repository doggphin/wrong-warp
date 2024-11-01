using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Networking
{
    public enum ClientNetMessageTypeId : byte
    {
        Heartbeat = 0,
        ConnectRequest = 1,
        Inputs = 2,
    }

    public enum ServerNetMessageTypeId : byte
    {
        ConnectResponse = 0,
        Disconnect = 1,
        AssignPlayerNetObject = 2,
        NetObjectUpdate = 3,
    }

    public interface INetMessage
    {
        public abstract byte[] Serialize();
        public abstract bool Deserialize(byte[] data, int readFrom = 0);
        public int GetSerializedLength();
    }
}
