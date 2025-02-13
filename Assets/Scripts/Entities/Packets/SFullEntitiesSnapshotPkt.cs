using LiteNetLib.Utils;

namespace Networking.Shared {
    public class SFullEntitiesSnapshotPkt : SPacket<SFullEntitiesSnapshotPkt> {
        public EntitySerializable[] entities;
        public bool isFullReset;

        public override void Deserialize(NetDataReader reader) {
            isFullReset = reader.GetBool();

            uint entitiesCount = reader.GetVarUInt();
            entities = new EntitySerializable[entitiesCount];
            for(int i=0; i<entitiesCount; i++) {
                entities[i] = new();
                entities[i].Deserialize(reader);
            }
        }


        public override void Serialize(NetDataWriter writer) {
            writer.Put(PacketIdentifier.SFullEntitiesSnapshot);

            writer.Put(isFullReset);
            writer.PutVarUInt((uint)entities.Length);
            foreach(EntitySerializable entity in entities) {
                entity.Serialize(writer);
            }
        }


        public override bool ShouldCache => true;
    }
}