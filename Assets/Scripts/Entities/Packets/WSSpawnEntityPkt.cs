using UnityEngine;
using LiteNetLib.Utils;
using Networking.Client;

namespace Networking.Shared {
    public enum WEntitySpawnReason : byte {
        Spawn,
        Load,
    }

    public class WSEntitySpawnPkt : SPacket<WSEntitySpawnPkt> {
        public WEntitySerializable entity;
        public WEntitySpawnReason reason;

        public override void Deserialize(NetDataReader reader) {
            entity.Deserialize(reader);
            reason = (WEntitySpawnReason)reader.GetByte();
        }


        public override void Serialize(NetDataWriter writer) {
            writer.Put(PacketIdentifier.SEntitySpawn);

            entity.Serialize(writer);
            writer.Put((byte)reason);
        }

        public override bool ShouldCache => true;
    }
}