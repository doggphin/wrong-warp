using LiteNetLib.Utils;
using UnityEngine;

namespace Networking.Shared {
    public class WSFullEntitiesSnapshotPkt : INetSerializable, IClientApplicablePacket {
        public WEntitySerializable[] entities;
        public bool isFullReset;

        public void Deserialize(NetDataReader reader) {
            isFullReset = reader.GetBool();

            uint entitiesCount = reader.GetVarUInt();
            entities = new WEntitySerializable[entitiesCount];
            for(int i=0; i<entitiesCount; i++) {
                entities[i] = new();
                entities[i].Deserialize(reader);
                Debug.Log(entities[i].entityId);
            }
        }

        public void Serialize(NetDataWriter writer) {
            writer.Put(WPacketType.SFullEntitiesSnapshot);

            writer.Put(isFullReset);
            writer.PutVarUInt((uint)entities.Length);
            foreach(WEntitySerializable entity in entities) {
                entity.Serialize(writer);
                Debug.Log(entity.entityId);
            }
        }

        public void ApplyOnClient(int tick)
        {
            Debug.Log("sup!");
        }
    }
}