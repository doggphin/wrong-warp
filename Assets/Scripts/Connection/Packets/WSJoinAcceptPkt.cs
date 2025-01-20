using UnityEngine;
using LiteNetLib.Utils;
using Networking.Client;
using TMPro;

namespace Networking.Shared {
    public class WSJoinAcceptPkt : INetPacketForClient {
        public string userName;
        public int tick;

        public void Serialize(NetDataWriter writer) {
            Debug.Log("Putting a join accept!");
            writer.Put(WPacketType.SJoinAccept);

            writer.Put(userName);
            writer.Put(tick);
        }

        public void Deserialize(NetDataReader reader) {
            userName = reader.GetString();
            tick = reader.GetInt();
        }

        // TODO: check if could just sent tick instead of including in join packet
        public bool ShouldCache => false;
        public void ApplyOnClient(int _)
        {
            WCNetClient.HandleJoinAccept(this);
        }
    }
}
