using LiteNetLib.Utils;
using Networking.Client;
using UnityEngine;

namespace Networking.Shared {
    public class WSSetPlayerEntityPkt : NetPacketForClient<WSSetPlayerEntityPkt> {
        public int entityId;

        public override void Deserialize(NetDataReader reader) {
            entityId = reader.GetInt();
        }

        public override void Serialize(NetDataWriter writer) {
            writer.Put(WPacketIdentifier.SSetPlayerEntity);

            writer.Put(entityId);     
        }

        public override bool ShouldCache => false;
    }
}