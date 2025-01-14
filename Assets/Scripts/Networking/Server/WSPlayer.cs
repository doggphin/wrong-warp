using System.Runtime.Serialization;
using LiteNetLib;
using LiteNetLib.Utils;
using Networking.Shared;
using Unity.VisualScripting;

namespace Networking.Server {
    public class WSPlayer {
        public static WSPlayer FromPeer(NetPeer peer) {
            return peer.Tag == null ? null : (WSPlayer)peer.Tag;
        }
        public static bool FromPeer(NetPeer peer, out WSPlayer player) {
            if(peer.Tag != null) {
                player = (WSPlayer)peer.Tag;
                return true;
            }

            player = null;
            return false;
        }

        public WSChunk previousChunk = null;
        public WSEntity Entity { get; private set; }
        public NetPeer Peer { get; private set; }
        public NetDataWriter writer = null;

        private bool isInitialized = false;

        public void Init(NetPeer peer, WSEntity entity) {
            if (isInitialized)
                return;

            Peer = peer;
            Entity = entity;
            writer = new();

            isInitialized = true;
        }

        
        public void AddData(INetSerializable serializable) {
            serializable.Serialize(writer);
        }
    }
}
