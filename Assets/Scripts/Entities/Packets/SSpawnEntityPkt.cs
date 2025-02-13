using UnityEngine;
using LiteNetLib.Utils;
using Networking.Client;

namespace Networking.Shared {
    public enum EntitySpawnReason : byte {
        Spawn,
        Load,
    }

    public class SEntitySpawnPkt : SPacket<SEntitySpawnPkt> {
        public EntitySerializable entity;
        public EntitySpawnReason reason;

        public override void Deserialize(NetDataReader reader) {
            entity = new();
            entity.Deserialize(reader);
            reason = (EntitySpawnReason)reader.GetByte();
        }


        public override void Serialize(NetDataWriter writer) {
            writer.Put(PacketIdentifier.SEntitySpawn);

            entity.Serialize(writer);
            writer.Put((byte)reason);
        }

        public override bool ShouldCache => true;
    }
}