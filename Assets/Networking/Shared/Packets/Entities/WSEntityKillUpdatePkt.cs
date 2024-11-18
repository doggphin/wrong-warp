using LiteNetLib.Utils;

namespace Networking.Shared {
    public enum WEntityKillReason : byte {
        Death,
        Despawn,
        Unload
    }

    public class WSEntityKillUpdatePkt : INetSerializable {
        public WEntityKillReason killReason;

        public void Deserialize(NetDataReader reader) {
            killReason = (WEntityKillReason)reader.GetByte();
        }

        public void Serialize(NetDataWriter writer) {
            writer.Put((byte)killReason);
        }
    }
}