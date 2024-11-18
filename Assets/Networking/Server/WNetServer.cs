using LiteNetLib;
using UnityEngine;
using LiteNetLib.Utils;
using System.Net;
using System.Net.Sockets;

using Networking.Shared;
using System.Collections.Generic;

namespace Networking.Server {
    public class WNetServer : MonoBehaviour, INetEventListener {
        public NetManager ServerNetManager { get; private set; }

        private NetDataWriter writer;

        public int Tick { get; private set; }
        public static WNetServer Instance { get; private set; }

        private void Awake() {
            if(Instance != null)
                Destroy(gameObject);

            Instance = this;
            DontDestroyOnLoad(gameObject);
            writer = new();

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
            WNetEntityManager.Instance.AdvanceTick(Tick);

            // If this tick isn't an update tick, advance to the next one
            if(Tick++ % WNetCommon.TICKS_PER_UPDATE != 0)
                return;


        }

        private NetDataWriter WriteSerializable<T>(WPacketType packetType, T packet) where T : INetSerializable {
            writer.Reset();
            writer.Put((ushort)packetType);
            packet.Serialize(writer);
            return writer;
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


        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod) {
            Debug.unityLogger.Log("Received a packet!");
            WPacketType packetType = (WPacketType)reader.GetUShort();

            switch (packetType) {
                case WPacketType.CJoin:
                    WCJoinRequestPkt joinRequest = new();
                    joinRequest.Deserialize(reader);
                    OnJoinReceived(joinRequest, peer);
                    break;
                default:
                    Debug.Log($"Could not handle packet of type {packetType}!");
                    break;
            }
        }


        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType) { }


        public void OnPeerConnected(NetPeer peer) {
            Debug.Log($"Player connected: {peer.Address}!");
        }


        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo) {
            Debug.Log($"Player disconnected: {disconnectInfo.Reason}!");

            WNetPlayer netPlayer = (WNetPlayer)peer.Tag;
            WNetEntityManager.Instance.KillEntity(netPlayer.Id);
        }
    

        private void OnJoinReceived(WCJoinRequestPkt joinRequest, NetPeer peer) {
            Debug.Log($"Join packet received for {joinRequest.userName}");

            WNetEntity playerEntity = WNetEntityManager.Instance.SpawnEntity(WNetPrefabId.Player);
            WNetPlayer netPlayer = playerEntity.GetComponent<WNetPlayer>();
            peer.Tag = netPlayer;

            peer.Send(WriteSerializable(WPacketType.SJoinAccept, new WSJoinAcceptPkt { userName = joinRequest.userName }), DeliveryMethod.ReliableOrdered);
        }


        private void OnDestroy() {
            ServerNetManager.Stop();
        }
    }
}
