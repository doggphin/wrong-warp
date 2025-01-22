using System.Collections.Generic;
using LiteNetLib.Utils;

namespace Networking.Shared {
    public class WSEntitiesLoadedDeltaPkt : SPacket<WSEntitiesLoadedDeltaPkt>
    {
        public List<int> entityIdsToRemove;
        public List<WEntitySerializable> entitiesToAdd;

        public override void Deserialize(NetDataReader reader)
        {
            int entityIdsToRemoveCount = (int)reader.GetVarUInt();
            entityIdsToRemove = new(entityIdsToRemoveCount);

            for(int i=0; i<entityIdsToRemoveCount; i++) {
                int entityId = reader.GetInt();

                entityIdsToRemove.Add(entityId);
            }

            int entitiesToAddCount = (int)reader.GetVarUInt();
            entitiesToAdd = new(entitiesToAddCount);

            for(int i=0; i<entitiesToAddCount; i++) {
                WEntitySerializable serializedEntity = new();
                serializedEntity.Deserialize(reader);

                entitiesToAdd.Add(serializedEntity);
            }
        }

        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(PacketIdentifier.SEntitiesLoadedDelta);

            writer.PutVarUInt((uint)entityIdsToRemove.Count);
            foreach(int entityId in entityIdsToRemove) {
                writer.Put(entityId);
            }

            writer.PutVarUInt((uint)entitiesToAdd.Count);
            foreach(WEntitySerializable entity in entitiesToAdd) {
                entity.Serialize(writer);
            }
        }

        public override bool ShouldCache => false;
    }
}
