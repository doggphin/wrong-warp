using LiteNetLib.Utils;
using Networking.Server;

namespace Networking.Shared {
    public class WEntitySerializable : INetSerializable {
        public int entityId;
        public NetPrefabType prefabId;
        public WTransformSerializable transform;

        public void Deserialize(NetDataReader reader) {
            entityId = reader.GetInt();
            prefabId = reader.GetPrefabId();
            transform.Deserialize(reader);
        }

        public void Serialize(NetDataWriter writer) {
            writer.Put(entityId);
            writer.Put(prefabId);
            transform.Serialize(writer);
        }
    }
}