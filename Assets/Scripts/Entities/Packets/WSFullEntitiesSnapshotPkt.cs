using LiteNetLib.Utils;
using Networking.Client;
using UnityEngine;

namespace Networking.Shared {
    public class WSFullEntitiesSnapshotPkt : INetPacketForClient {
        public WEntitySerializable[] entities;
        public bool isFullReset;

        public void Deserialize(NetDataReader reader) {
            isFullReset = reader.GetBool();

            uint entitiesCount = reader.GetVarUInt();
            entities = new WEntitySerializable[entitiesCount];
            for(int i=0; i<entitiesCount; i++) {
                entities[i] = new();
                entities[i].Deserialize(reader);
            }
        }

        public void Serialize(NetDataWriter writer) {
            writer.Put(WPacketIdentifier.SFullEntitiesSnapshot);

            writer.Put(isFullReset);
            writer.PutVarUInt((uint)entities.Length);
            foreach(WEntitySerializable entity in entities) {
                entity.Serialize(writer);
            }
        }

        public bool ShouldCache =>true;
        public void ApplyOnClient(int tick) {
            WCEntityManager.HandleFullEntitiesSnapshot(this);
        }
    }
}