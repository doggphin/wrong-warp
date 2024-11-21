using LiteNetLib;
using Networking.Shared;

namespace Networking.Server {
    public class WNetServerPlayer {
        public static WNetServerPlayer FromPeer(NetPeer peer) {
            return (WNetServerPlayer)peer.Tag;
        }

        public WNetServerEntity Entity { get; private set; }
        public NetPeer Peer { get; private set; }

        private bool isInitialized = false;

        public void Init(NetPeer peer, WNetServerEntity entity) {
            if (isInitialized)
                return;

            Peer = peer;
            Entity = entity;

            isInitialized = true;
        }

    }
}
