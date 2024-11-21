using LiteNetLib.Utils;

namespace Networking.Shared {
    public enum WEntitySpawnReason : byte {
        Spawn,
        Load,
    }

    public class WSEntitySpawnPkt : INetSerializable {
        WEntitySerializable entity;
        WEntitySpawnReason reason;

        public void Deserialize(NetDataReader reader) {
            entity.Deserialize(reader);
            reason = (WEntitySpawnReason)reader.GetByte();
        }

        public void Serialize(NetDataWriter writer) {
            writer.Put((ushort)WPacketType.SEntitySpawn);

            entity.Serialize(writer);
            writer.Put((byte)reason);
        }
    }
}