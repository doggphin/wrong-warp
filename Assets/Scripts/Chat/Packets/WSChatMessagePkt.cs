using LiteNetLib.Utils;
using UnityEngine;

namespace Networking.Shared {
    public class WSChatMessagePkt : INetPacketForClient {
        public string message;
        public bool isServerMessage;

        // If this is a server message, it doesn't originate from a speaker
        public int speakerId;

        public void Deserialize(NetDataReader reader) {
            message = reader.GetString();
            isServerMessage = reader.GetBool();
            if(!isServerMessage)
                speakerId = reader.GetInt();
        }


        public void Serialize(NetDataWriter writer) {
            writer.Put(WPacketIdentifier.SChatMessage);

            writer.Put(message);
            writer.Put(isServerMessage);
            if(!isServerMessage)
                writer.Put(speakerId);
        }

        public bool ShouldCache => true;
        public void ApplyOnClient(int _) {
            ChatUiManager.ReceiveChatMessage(this);
        }
    }
}