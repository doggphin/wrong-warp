using LiteNetLib.Utils;
using UnityEngine;

namespace Networking.Shared {
    public class CJoinRequestPkt : CPacket<CJoinRequestPkt> {
        public const int MAX_USERNAME_SIZE = 24;
        public string userName;

        public override void Deserialize(NetDataReader reader) {
            userName = reader.GetString(MAX_USERNAME_SIZE) ?? "";
        }

        public override void Serialize(NetDataWriter writer) {
            writer.Put(PacketIdentifier.CJoinRequest);

            writer.Put(userName);     
        }
    }
}