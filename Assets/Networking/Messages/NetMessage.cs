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


    public static class NetMessageDeserializerFactory
    {
        public ClientNetMessage FromClientMessage(byte[] data, uint tick, uint playerId)
        {
            ClientNetMessageType netMsgType;
            if (data.Length >= 2) { 
                netMsgType = (ClientNetMessageType)BitConverter.ToUInt16(data, 0));
            } else {
                return null;
            }

            switch(netMsgType)
            {
                case ClientNetMessageType.Heartbeat:
                    return new CNM_Inputs(playerId);
                    
            }
        }
    }

    public class NetMessageBase
    {
        public uint tick;
        public virtual byte[] Serialize() { throw new NotImplementedException(); }
        public virtual void Apply() { throw new NotImplementedException(); }
        public virtual bool Deserialize(byte[] data) { throw new NotImplementedException(); }
    }

    public class ClientNetMessage : NetMessageBase
    {
        public uint playerId;

        public ClientNetMessage(uint playerId) {
            this.playerId = playerId;
        }
    }

    public class ServerNetMessage : NetMessageBase
    {

    }
}
