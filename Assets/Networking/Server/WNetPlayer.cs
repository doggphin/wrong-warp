using LiteNetLib;
using UnityEngine;

using Networking.Shared;

namespace Networking.Server {
    public class WNetPlayer {
        public static WNetPlayer FromPeer(NetPeer peer) {
            return (WNetPlayer)peer.Tag;
        }

        public WNetEntity Entity { get; private set; }
        public NetPeer Peer { get; private set; }

        private bool isInitialized = false;

        public void Init(NetPeer peer, WNetEntity entity) {
            if (isInitialized)
                return;

            Peer = peer;
            Entity = entity;

            isInitialized = true;
        }

    }
}
