using LiteNetLib;
using UnityEngine;
using LiteNetLib.Utils;
using System.Net;
using System.Net.Sockets;

using Networking.Shared;

namespace Networking.Server {
    public class WNetServer : MonoBehaviour, INetEventListener {
        public NetManager ServerNetManager { get; private set; }

        private NetDataWriter writer = new();

        public int Tick { get; private set; }
        public static WNetServer Instance { get; private set; }

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


        public void FixedUpdate() {
            AdvanceTick();
        }


        public void StartServer() {
            ServerNetManager.Start(WNetCommon.WRONGWARP_PORT);

            Tick = 0;
            Debug.Log($"Running server on port {WNetCommon.WRONGWARP_PORT}!");
        }


        public void AdvanceTick() {
            WNetServerEntityManager.AdvanceTick(Tick);

            WNetServerChunkManager.UnloadChunksMarkedForUnloading();

            // If this tick isn't an update tick, advance to the next one
            if (Tick++ % WNetCommon.TICKS_PER_SNAPSHOT != 0)
                return;

            foreach (var peer in ServerNetManager.ConnectedPeerList) {
                WNetServerPlayer netPlayer = WNetServerPlayer.FromPeer(peer);
                if (netPlayer == null)
                    continue;
                // Check the current player's chunk to see if the writer has already been written
                WNetChunk chunk = netPlayer.Entity.CurrentChunk;
                NetDataWriter chunkWriter = chunk.GetPrepared3x3SnapshotPacket();

                peer.Send(chunkWriter, DeliveryMethod.Unreliable);
            }

            WNetServerChunkManager.ResetChunkUpdatesAndSnapshots();
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
            WNetPacketComms.ReadMultiPacket(peer, reader, ProcessPacketFromReader, true);
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

            WNetServerEntity playerEntity = WNetServerEntityManager.SpawnEntity(WNetPrefabId.Player, true);

            WNetServerPlayer netPlayer = new WNetServerPlayer();
            netPlayer.Init(peer, playerEntity);

            peer.Tag = netPlayer;

            WNetPacketComms.SendSingle(writer, peer, Tick, new WSJoinAcceptPkt { userName = joinRequest.userName }, DeliveryMethod.ReliableOrdered);
        }


        private void OnDestroy() {
            ServerNetManager.Stop();
        }
    }
}
