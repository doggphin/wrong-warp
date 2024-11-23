using LiteNetLib.Utils;

namespace Networking.Shared {
    public class WSFullEntitiesSnapshotPkt : INetSerializable {
        WEntitySerializable[] entities;
        bool isFullReset;

        public void Deserialize(NetDataReader reader) {
            isFullReset = reader.GetBool();

            uint entitiesCount = reader.GetVarUInt();

            entities = new WEntitySerializable[entitiesCount];
            for(int i=0; i<entitiesCount; i++) {
                entities[i].Deserialize(reader);
            }
        }

        public void Serialize(NetDataWriter writer) {
            writer.Put((ushort)WPacketType.SFullEntitiesSnapshot);

            writer.Put(isFullReset);
            writer.PutVarUInt((uint)entities.Length);
            foreach(WEntitySerializable entity in entities) {
                entity.Serialize(writer);
            }
        }
    }
}