using LiteNetLib.Utils;
using UnityEngine;

namespace Networking.Shared {
    public class WCJoinRequestPkt : INetSerializable {
        public string userName;

        public void Serialize(NetDataWriter writer) {
            writer.Put(userName);
        }

        public void Deserialize(NetDataReader reader) {
            userName = reader.GetString();
        }
    }
}