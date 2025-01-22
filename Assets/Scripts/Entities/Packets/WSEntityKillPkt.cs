using LiteNetLib.Utils;
using Networking.Client;

namespace Networking.Shared {
    public enum WEntityKillReason : byte {
        Death,
        Despawn,
        Unload
    }

    public class WSEntityKillPkt : SPacket<WSEntityKillPkt> {
        public int entityId;
        public WEntityKillReason reason;

        public override void Deserialize(NetDataReader reader) {
            entityId = reader.GetInt();
            reason = (WEntityKillReason)reader.GetByte();
        }


        public override void Serialize(NetDataWriter writer) {
            writer.Put(PacketIdentifier.SEntityKill);

            writer.Put(entityId);
            writer.Put((byte)reason);
        }

        public override bool ShouldCache => true;
    }
}