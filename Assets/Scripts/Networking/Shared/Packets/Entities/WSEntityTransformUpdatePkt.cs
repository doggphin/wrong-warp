using LiteNetLib.Utils;
using System;
using UnityEngine;

namespace Networking.Shared {
    public class WSEntityTransformUpdatePkt : INetSerializable {
        public int entityId;
        public WTransformSerializable transform;

        public void Serialize(NetDataWriter writer) {
            writer.Put((ushort)WPacketType.SEntityTransformUpdate);

            transform.Serialize(writer);
        }

        public void Deserialize(NetDataReader reader) {
            transform.Deserialize(reader);
        }
    }
}
