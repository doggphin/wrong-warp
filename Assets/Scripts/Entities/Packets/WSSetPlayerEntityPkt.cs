using LiteNetLib.Utils;
using Networking.Client;
using UnityEngine;

namespace Networking.Shared {
    public class WSSetPlayerEntityPkt : INetPacketForClient {
        public int entityId;

        public void Deserialize(NetDataReader reader) {
            entityId = reader.GetInt();
        }

        public void Serialize(NetDataWriter writer) {
            writer.Put(WPacketType.SSetPlayerEntity);

            writer.Put(entityId);     
        }

        public bool ShouldCache => false;
        public void ApplyOnClient(int _)
        {
            WCNetClient.HandleSetPlayerEntity(this);
        }
    }
}