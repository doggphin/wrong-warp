using System.Collections.Generic;
using System.Runtime.Serialization;
using LiteNetLib;
using LiteNetLib.Utils;
using Networking.Shared;
using Unity.VisualScripting;

namespace Networking.Server {
    public static class SPlayerExtensions {
        public static bool TryGetSPlayer(this NetPeer peer, out SPlayer player) {
            // If peer is null, then this is the host
            if(peer == null) {
                player = SNetManager.HostPlayer;
            } else {
                player = peer.Tag as SPlayer;
            }

            return player != null;
        }
    }

    public class SPlayer {
        public static SPlayer FromPeer(NetPeer peer) {
            return peer == null ?
                SNetManager.HostPlayer :
                peer.Tag as SPlayer;
        }

        public static bool TryFromPeer(NetPeer peer, out SPlayer player) {
            player = FromPeer(peer);
            return player != null;
        }

        public SChunk previousChunk = null;
        public SEntity Entity { get; private set; }
        public NetPeer Peer { get; private set; }
        public bool IsHost => Peer == null;

        ///<summary> ! Is null on host players ! </summary>
        public TickedPacketCollection ReliablePackets { get; private set; }

        public SPlayer(NetPeer peer) {
            Peer = peer;
            // Only instantiate ReliablePackets if non-host player
            ReliablePackets = peer == null ?
                null :
                new();
        }
        
        ///<summary> Sets the entity of this player and sends a notification to the peer that their entity has changed. </summary>
        /*
        public void SetEntity(SEntity entity) {
            if(ReferenceEquals(entity, Entity))
                return;
            
            if(Entity != null)
                Entity.SetPlayer(null);

            entity.SetPlayer(this);
            Entity = entity;
            
            // Notify player that their entity has changed
            SSetPlayerEntityPkt setPlayerPkt = new() {
                entityId = entity.Id
            };

            ReliablePackets?.AddPacket(SNetManager.Tick, setPlayerPkt);  
        }
        */
        
        public void HandleSetEntity(SEntity entity) {
            if(ReferenceEquals(entity, Entity)) {
                return;
            }

            Entity = entity;
            
            if(entity == null) {
                // TODO: Send unset player packet
            } else {
                // Send set player packet
                SSetPlayerEntityPkt setPlayerPkt = new() {
                    entityId = entity.Id
                };

                ReliablePackets?.AddPacket(SNetManager.Tick, setPlayerPkt);
            }
        }
    }
}
