using LiteNetLib.Utils;
using Networking.Client;
using UnityEngine;

namespace Networking.Shared {
    public class SFullEntitiesSnapshotPkt : SPacket<SFullEntitiesSnapshotPkt> {
        public WEntitySerializable[] entities;
        public bool isFullReset;

        public override void Deserialize(NetDataReader reader) {
            isFullReset = reader.GetBool();

            uint entitiesCount = reader.GetVarUInt();
            entities = new WEntitySerializable[entitiesCount];
            for(int i=0; i<entitiesCount; i++) {
                entities[i] = new();
                entities[i].Deserialize(reader);
            }
        }

        public override void Serialize(NetDataWriter writer) {
            writer.Put(PacketIdentifier.SFullEntitiesSnapshot);

            writer.Put(isFullReset);
            writer.PutVarUInt((uint)entities.Length);
            foreach(WEntitySerializable entity in entities) {
                entity.Serialize(writer);
            }
        }

        public override bool ShouldCache =>true;
    }
}