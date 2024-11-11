using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Networking
{
    public enum ServerNetMessageType : ushort
    {
        ConnectResponse          = 65535,
        Disconnect               = 65534,
        AssignPlayerNetObject    = 65533,
        NetObjectUpdate          = 65532,
    }


    public enum ClientNetMessageType : ushort
    {
        Heartbeat        = 0,
        ConnectRequest   = 1,
        Inputs           = 2,
    }


    public class NetMessageBase
    {
        public uint tick;
        public virtual byte[] Serialize() { throw new NotImplementedException(); }
        public virtual void Apply() { throw new NotImplementedException(); }
        public virtual int? Deserialize(byte[] data, int readFrom) { throw new NotImplementedException(); }
    }


    public abstract class ClientNetMessage : NetMessageBase
    {
        public uint playerId;
        public ConnectionType type;

        public ClientNetMessage(uint playerId) {
            this.playerId = playerId;
        }
    }


    public abstract class ServerNetMessage : NetMessageBase
    {

    }
}
