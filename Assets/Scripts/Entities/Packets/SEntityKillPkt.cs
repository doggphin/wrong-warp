using LiteNetLib.Utils;
using Networking.Client;

namespace Networking.Shared {
    public enum EntityKillReason : byte {
        Death,
        Despawn,
        Unload
    }

    public class SEntityKillPkt : SPacket<SEntityKillPkt> {
        public int entityId;
        public EntityKillReason reason;

        public override void Deserialize(NetDataReader reader) {
            entityId = reader.GetInt();
            reason = (EntityKillReason)reader.GetByte();
        }


        public override void Serialize(NetDataWriter writer) {
            writer.Put(PacketIdentifier.SEntityKill);

            writer.Put(entityId);
            writer.Put((byte)reason);
        }

        public override bool ShouldCache => true;
    }
}