using LiteNetLib.Utils;
using Networking.Client;
using System;
using UnityEngine;

namespace Networking.Shared {
    public class WSEntityTransformUpdatePkt : INetSerializable, IClientApplicablePacket {
        public int entityId;
        public WTransformSerializable transform;

        public void Deserialize(NetDataReader reader) {
            transform.Deserialize(reader);
        }

        public void Serialize(NetDataWriter writer) {
            writer.Put(WPacketType.SEntityTransformUpdate);

            transform.Serialize(writer);
        }

        public void ApplyOnClient(int tick) {
            WCEntityManager.SetEntityTransformForTick(tick, this);
        }
    }
}
