using LiteNetLib.Utils;
using UnityEngine;

namespace Networking.Shared {
    public class WSSetPlayerEntityPkt : INetSerializable, IClientApplicablePacket {
        public int entityId;

        public void Deserialize(NetDataReader reader) {
            entityId = reader.GetInt();
        }

        public void Serialize(NetDataWriter writer) {
            writer.Put(WPacketType.SSetPlayerEntity);

            writer.Put(entityId);     
        }

        public void ApplyOnClient(int tick)
        {
            throw new System.NotImplementedException();
        }
    }
}