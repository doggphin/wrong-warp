using LiteNetLib;
using Networking.Shared;

namespace Networking.Server {
    public class WSPlayer {
        public static WSPlayer FromPeer(NetPeer peer) {
            return (WSPlayer)peer.Tag;
        }
        public WSChunk previousChunk = null;
        public WSEntity Entity { get; private set; }
        public NetPeer Peer { get; private set; }

        private bool isInitialized = false;

        public void Init(NetPeer peer, WSEntity entity) {
            if (isInitialized)
                return;

            Peer = peer;
            Entity = entity;

            isInitialized = true;
        }

    }
}
