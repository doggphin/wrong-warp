using LiteNetLib.Utils;
using UnityEngine;

namespace Code.Shared {
    public enum WPacketType : ushort {
        Unimplemented,

        CJoin,
        SJoinAccept,
    }


    public class WCJoinPacket : INetSerializable {
        public string userName;

        public void Serialize(NetDataWriter writer) {
            writer.Put(userName);
        }

        public void Deserialize(NetDataReader reader) {
            userName = reader.GetString();
        }
    }


    public class WSJoinAcceptPacket : INetSerializable {
        public string userName;

        public void Serialize(NetDataWriter writer) {
            writer.Put(userName);
        }

        public void Deserialize(NetDataReader reader) {
            userName = reader.GetString();
        }
    }
}