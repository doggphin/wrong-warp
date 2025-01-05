using LiteNetLib.Utils;
using UnityEngine;

namespace Networking.Shared {
    public class WCJoinRequestPkt : INetSerializable {
        public bool s_isValid;

        public string userName;

        public void Deserialize(NetDataReader reader) {
            userName = reader.GetString();

            if (userName != null)
                s_isValid = true;
        }

        public void Serialize(NetDataWriter writer) {
            writer.Put(WPacketType.CJoinRequest);

            writer.Put(userName);     
        }
    }
}