using LiteNetLib.Utils;
using UnityEngine;

namespace Networking.Shared {
    public class WSChatMessagePkt : INetSerializable, IClientApplicablePacket {
        public string message;
        public bool isServerMessage;

        // If this is a server message, it doesn't originate from a speaker
        public int speakerId;

        public void Serialize(NetDataWriter writer) {
            writer.Put((ushort)WPacketType.SChatMessage);

            writer.Put(message);

            writer.Put(isServerMessage);

            if(!isServerMessage) {
                writer.Put(speakerId);
            }
        }

        public void Deserialize(NetDataReader reader) {
            message = reader.GetString();
            speakerId = reader.GetInt();

            bool isServerMessage = reader.GetBool();
            if(isServerMessage)
                speakerId = reader.GetInt();
        }

        public void ApplyOnClient(int _) {
            ChatUiManager.ReceiveChatMessage(this);
        }
    }
}