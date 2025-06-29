using UnityEngine;
using LiteNetLib.Utils;
using Networking.Client;
using TMPro;

namespace Networking.Shared {
    public class SJoinAcceptPkt : SPacket<SJoinAcceptPkt> {
        public string userName;
        public int tick;

        public override void Serialize(NetDataWriter writer) {
            Debug.Log("Putting a join accept!");
            writer.Put(PacketIdentifier.SJoinAccept);

            writer.Put(userName);
            writer.Put(tick);
        }

        public override void Deserialize(NetDataReader reader) {
            userName = reader.GetString();
            tick = reader.GetInt();
        }

        // TODO: check if could just sent tick instead of including in join packet
        public override bool ShouldCache => false;
    }
}
