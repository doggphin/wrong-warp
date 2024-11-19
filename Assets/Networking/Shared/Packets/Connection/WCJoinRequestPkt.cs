using LiteNetLib.Utils;
using UnityEngine;

namespace Networking.Shared {
    public class WCJoinRequestPkt : INetSerializable {
        public string userName;

        public bool s_isValid;

        public void Serialize(NetDataWriter writer) {
            writer.Put(userName);

            if(userName == null)
                s_isValid = false;
        }

        public void Deserialize(NetDataReader reader) {
            userName = reader.GetString();
        }
    }
}