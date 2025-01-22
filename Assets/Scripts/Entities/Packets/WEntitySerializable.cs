using LiteNetLib.Utils;
using Networking.Server;

namespace Networking.Shared {
    public class WEntitySerializable : INetSerializable {
        public int entityId;
        public EntityPrefabId entityPrefabId;
        public TransformSerializable transform;

        public void Deserialize(NetDataReader reader) {
            entityId = reader.GetInt();
            entityPrefabId = reader.GetPrefabId();
            transform.Deserialize(reader);
        }

        public void Serialize(NetDataWriter writer) {
            writer.Put(entityId);
            writer.Put(entityPrefabId);
            transform.Serialize(writer);
        }
    }
}