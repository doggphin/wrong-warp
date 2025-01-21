using LiteNetLib.Utils;
using UnityEngine;

namespace Networking.Shared {
    public class WSChatMessagePkt : NetPacketForClient<WSChatMessagePkt> {
        public string message;
        public bool isServerMessage;

        // If this is a server message, it doesn't originate from a speaker
        public int speakerId;

        public override void Deserialize(NetDataReader reader) {
            message = reader.GetString();
            isServerMessage = reader.GetBool();
            if(!isServerMessage)
                speakerId = reader.GetInt();
        }


        public override void Serialize(NetDataWriter writer) {
            writer.Put(WPacketIdentifier.SChatMessage);

            writer.Put(message);
            writer.Put(isServerMessage);
            if(!isServerMessage)
                writer.Put(speakerId);
        }

        public override bool ShouldCache => true;
    }
}