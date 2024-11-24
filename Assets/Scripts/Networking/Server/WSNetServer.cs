using LiteNetLib;
using UnityEngine;
using LiteNetLib.Utils;
using System.Net;
using System.Net.Sockets;

using Networking.Shared;
using Controllers.Shared;

namespace Networking.Server {
    public class WSNetServer : MonoBehaviour, INetEventListener {
        public NetManager ServerNetManager { get; private set; }

        private NetDataWriter writer = new();

        private int tick;
        public static int Tick => Instance.tick;

        public static WSNetServer Instance { get; private set; }

        private void Awake() {
            if(Instance != null)
                Destroy(gameObject);

            Instance = this;
            DontDestroyOnLoad(gameObject);

            ServerNetManager = new NetManager(this) {
                AutoRecycle = true,
                IPv6Enabled = false
            };
        }


        private void Update() {
            ServerNetManager.PollEvents();
        }
        

        public void StartServer() {
            ServerNetManager.Start(WCommon.WRONGWARP_PORT);

            tick = 0;
            Debug.Log($"Running server on port {WCommon.WRONGWARP_PORT}!");

            WSEntity entity = WSEntityManager.SpawnEntity(WPrefabId.Test, true);
            entity.gameObject.AddComponent<SpinnerTest>();

            WSEntity spectator = WSEntityManager.SpawnEntity(WPrefabId.Spectator, true);
            SpectatorController spectatorController = spectator.GetComponent<SpectatorController>();

            ControlsManager.mainControllable = spectatorController;
            ControlsManager.mainRotatable = spectatorController;
            
            spectatorController.EnableRotator();
            spectatorController.EnableController();
        }


        public void AdvanceTick() {
            WSEntityManager.AdvanceTick(Tick);

            ControlsManager.Poll(null);

            // If this tick isn't an update tick, advance to the next one
            if (tick++ % WCommon.TICKS_PER_SNAPSHOT != 0)
                return;

            foreach (var peer in ServerNetManager.ConnectedPeerList) {
                WSPlayer netPlayer = WSPlayer.FromPeer(peer);
                if (netPlayer == null)
                    continue;
                
                if(netPlayer.previousChunk != null) {
                    WSEntitiesLoadedDeltaPkt entitiesLoadedDeltaPkt = WSChunkManager.GetEntitiesLoadedDeltaPkt(
                        netPlayer.previousChunk.Coords,
                        netPlayer.Entity.ChunkPosition);

                    if(entitiesLoadedDeltaPkt != null) {
                        writer.Reset();
                        WPacketComms.StartMultiPacket(writer, tick);
                        WPacketComms.AddToMultiPacket(writer, entitiesLoadedDeltaPkt);

                        peer.Send(writer, DeliveryMethod.ReliableUnordered);
                    }
                }

                WSChunk chunk = netPlayer.Entity.CurrentChunk;
                NetDataWriter chunkWriter = chunk.GetPrepared3x3SnapshotPacket();
                peer.Send(chunkWriter, DeliveryMethod.Unreliable);

                netPlayer.previousChunk = chunk;
            }

            WSChunkManager.UnloadChunksMarkedForUnloading();
            WSChunkManager.ResetChunkUpdatesAndSnapshots();
        }


        public void OnConnectionRequest(ConnectionRequest request) {
            Debug.unityLogger.Log("Received a connection request!");
            request.AcceptIfKey("WW 0.01");
        }


        public void OnNetworkError(IPEndPoint endPoint, SocketError socketError) {
            Debug.Log($"Network error: {socketError}");
        }


        public void OnNetworkLatencyUpdate(NetPeer peer, int latency) {

        }


        private bool ProcessPacketFromReader(
            NetPeer peer,
            NetDataReader reader,
            int tick,
            WPacketType packetType) {

            switch (packetType) {

                case WPacketType.CJoinRequest:
                    WCJoinRequestPkt joinRequest = new();
                    joinRequest.Deserialize(reader);
                    if (!joinRequest.s_isValid)
                        return false;
                    OnJoinReceived(joinRequest, peer);
                    return true;

                default:
                    Debug.Log($"Could not handle packet of type {packetType}!");
                    return false;
            }
        }

        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod) {
            WPacketComms.ReadMultiPacket(peer, reader, ProcessPacketFromReader, true);
        }


        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType) { }


        public void OnPeerConnected(NetPeer peer) {
            Debug.Log($"Player connected: {peer.Address}!");
        }


        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo) {
            Debug.Log($"Player disconnected: {disconnectInfo.Reason}!");

            //WNetPlayer netPlayer = (WNetPlayer)peer.Tag;
            //WNetEntityManager.KillEntity(netPlayer.Entity.Id);
        }


        private void OnJoinReceived(WCJoinRequestPkt joinRequest, NetPeer peer) {
            Debug.Log($"Join packet received for {joinRequest.userName}");

            WSEntity playerEntity = WSEntityManager.SpawnEntity(WPrefabId.Player, true);
            
            WSPlayer netPlayer = new WSPlayer();
            netPlayer.Init(peer, playerEntity);

            peer.Tag = netPlayer;

            WSJoinAcceptPkt joinAcceptPacket = new() {
                userName = joinRequest.userName,
                playerEntityId = playerEntity.Id,
                tick = Tick - 10
            };

            WSEntitiesLoadedDeltaPkt entitiesLoadPacket = WSChunkManager.GetEntitiesLoadedDeltaPkt(null, Vector2Int.zero);
            
            WPacketComms.StartMultiPacket(writer, tick);
            WPacketComms.AddToMultiPacket(writer, joinAcceptPacket);
            WPacketComms.AddToMultiPacket(writer, entitiesLoadPacket);
            peer.Send(writer, DeliveryMethod.ReliableUnordered);
        }


        private void OnDestroy() {
            ServerNetManager.Stop();
        }
    }
}
