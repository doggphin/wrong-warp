using LiteNetLib.Utils;

namespace Networking.Shared {
    public class WEntitySerializable : INetSerializable {
        public int entityId;
        public WNetPrefabId prefabId;
        public WTransformSerializable transform;

        public void Deserialize(NetDataReader reader) {
            entityId = reader.GetInt();
            prefabId = (WNetPrefabId)reader.GetUShort();
            transform.Deserialize(reader);
        }

        public void Serialize(NetDataWriter writer) {
            writer.Put(entityId);
            writer.Put((ushort)prefabId);
            transform.Serialize(writer);
        }
    }
}