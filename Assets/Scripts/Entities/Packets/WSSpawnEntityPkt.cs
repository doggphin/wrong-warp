using UnityEngine;
using LiteNetLib.Utils;
using Networking.Client;

namespace Networking.Shared {
    public enum WEntitySpawnReason : byte {
        Spawn,
        Load,
    }

    public class WSEntitySpawnPkt : INetPacketForClient {
        public WEntitySerializable entity;
        public WEntitySpawnReason reason;

        public void Deserialize(NetDataReader reader) {
            entity.Deserialize(reader);
            reason = (WEntitySpawnReason)reader.GetByte();
        }


        public void Serialize(NetDataWriter writer) {
            writer.Put(WPacketType.SEntitySpawn);

            entity.Serialize(writer);
            writer.Put((byte)reason);
        }

        public bool ShouldCache => true;
        public void ApplyOnClient(int _)
        {
            WCEntityManager.Spawn(this);
        }
    }
}