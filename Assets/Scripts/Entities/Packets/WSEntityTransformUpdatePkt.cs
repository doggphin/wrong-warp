using LiteNetLib.Utils;
using Networking.Client;
using System;
using UnityEngine;

namespace Networking.Shared {
    public class WSEntityTransformUpdatePkt : INetEntityUpdatePacketForClient {
        public WTransformSerializable transform;
        public int EntityId { get; set; }
        
        public void Deserialize(NetDataReader reader) {
            transform.Deserialize(reader);
        }

        public void Serialize(NetDataWriter writer) {
            writer.Put(WPacketType.SEntityTransformUpdate);

            transform.Serialize(writer);
        }

        public bool ShouldCache => true;

        public void ApplyOnClient(int tick) {
            WCEntityManager.SetEntityTransformForTick(tick, this);
        }
    }
}
