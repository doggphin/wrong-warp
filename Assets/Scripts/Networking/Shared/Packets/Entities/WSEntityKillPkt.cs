using LiteNetLib.Utils;

namespace Networking.Shared {
    public enum WEntityKillReason : byte {
        Death,
        Despawn,
        Unload
    }

    public class WSEntityKillPkt : INetSerializable {
        public int entityId;
        public WEntityKillReason reason;

        public void Deserialize(NetDataReader reader) {
            entityId = reader.GetInt();
            reason = (WEntityKillReason)reader.GetByte();
        }

        public void Serialize(NetDataWriter writer) {
            writer.Put((ushort)WPacketType.SEntityKill);

            writer.Put(entityId);
            writer.Put((byte)reason);
        }
    }
}