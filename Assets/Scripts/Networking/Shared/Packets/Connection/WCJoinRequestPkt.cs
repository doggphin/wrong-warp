using LiteNetLib.Utils;
using UnityEngine;

namespace Networking.Shared {
    public class WCJoinRequestPkt : INetSerializable {
        public const int MAX_USERNAME_SIZE = 24;
        public bool s_isValid;

        public string userName;

        public void Deserialize(NetDataReader reader) {
            userName = reader.GetString(MAX_USERNAME_SIZE);

            if (userName != null)
                s_isValid = true;
        }

        public void Serialize(NetDataWriter writer) {
            writer.Put(WPacketType.CJoinRequest);

            writer.Put(userName);     
        }
    }
}