using LiteNetLib.Utils;
using Networking.Client;
using UnityEngine;

namespace Networking.Shared {
    public class WSSetPlayerEntityPkt : SPacket<WSSetPlayerEntityPkt> {
        public int entityId;

        public override void Deserialize(NetDataReader reader) {
            entityId = reader.GetInt();
        }

        public override void Serialize(NetDataWriter writer) {
            writer.Put(PacketIdentifier.SSetPlayerEntity);

            writer.Put(entityId);     
        }

        public override bool ShouldCache => false;
    }
}