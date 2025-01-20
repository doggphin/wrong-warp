using System.Collections.Generic;
using LiteNetLib.Utils;
using Networking.Client;
using UnityEngine;

namespace Networking.Shared {
    public class WSEntitiesLoadedDeltaPkt : INetPacketForClient
    {
        public List<int> entityIdsToRemove;
        public List<WEntitySerializable> entitiesToAdd;

        public void Deserialize(NetDataReader reader)
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

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(WPacketIdentifier.SEntitiesLoadedDelta);

            writer.PutVarUInt((uint)entityIdsToRemove.Count);
            foreach(int entityId in entityIdsToRemove) {
                writer.Put(entityId);
            }

            writer.PutVarUInt((uint)entitiesToAdd.Count);
            foreach(WEntitySerializable entity in entitiesToAdd) {
                entity.Serialize(writer);
            }
        }

        public bool ShouldCache => false;
        public void ApplyOnClient(int tick)
        {
            WCNetClient.HandleEntitiesLoadedDelta(tick, this);
        }
    }
}
