using UnityEngine;
using LiteNetLib.Utils;
using Networking.Client;

namespace Networking.Shared {
    public enum WEntitySpawnReason : byte {
        Spawn,
        Load,
    }

    public class WSEntitySpawnPkt : INetSerializable, IClientApplicablePacket {
        public WEntitySerializable entity;
        public WEntitySpawnReason reason;

        public void Deserialize(NetDataReader reader) {
            entity.Deserialize(reader);
            reason = (WEntitySpawnReason)reader.GetByte();
        }


        public void Serialize(NetDataWriter writer) {
            writer.Put((ushort)WPacketType.SEntitySpawn);

            entity.Serialize(writer);
            writer.Put((byte)reason);
        }

        
        public void ApplyOnClient(int tick)
        {
            Debug.Log("Spawning!");
            WCEntityManager.Spawn(this);
        }
    }
}