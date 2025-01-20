using LiteNetLib.Utils;
using Networking.Client;

namespace Networking.Shared {
    public enum WEntityKillReason : byte {
        Death,
        Despawn,
        Unload
    }

    public class WSEntityKillPkt : INetPacketForClient {
        public int entityId;
        public WEntityKillReason reason;

        public void Deserialize(NetDataReader reader) {
            entityId = reader.GetInt();
            reason = (WEntityKillReason)reader.GetByte();
        }


        public void Serialize(NetDataWriter writer) {
            writer.Put(WPacketIdentifier.SEntityKill);

            writer.Put(entityId);
            writer.Put((byte)reason);
        }

        public bool ShouldCache => true;
        public void ApplyOnClient(int _)
        {
            WCEntityManager.KillEntity(this);
        }
    }
}