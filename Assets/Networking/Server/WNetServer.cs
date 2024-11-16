using LiteNetLib;
using UnityEngine;
using LiteNetLib.Utils;
using System.Net;
using System.Net.Sockets;

using Networking.Shared;

namespace Networking.Server {
    public class WNetServer : MonoBehaviour, INetEventListener {
        private NetManager netManager;

        private NetDataWriter writer;

        public int Tick { get; private set; }

        private void Awake() {
            DontDestroyOnLoad(gameObject);
            writer = new();

            netManager = new NetManager(this) {
                AutoRecycle = true,
                IPv6Enabled = false
            };
        }


        private void Update() {
            netManager.PollEvents();
        }


        public void StartServer() {
            netManager.Start(WNetCommon.WRONGWARP_PORT);

            Tick = 0;
            Debug.Log($"Running server on port {WNetCommon.WRONGWARP_PORT}!");
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
                    WCJoinPacket joinPacket = new();
                    joinPacket.Deserialize(reader);
                    OnJoinReceived(joinPacket, peer);
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
        }
    

        private void OnJoinReceived(WCJoinPacket joinPacket, NetPeer peer) {
            Debug.Log($"Join packet received for {joinPacket.userName}");
            WNetEntity playerEntity = WNetEntityManager.SpawnEntity(WNetPrefabId.Player);
            peer.Send(WriteSerializable(WPacketType.SJoinAccept, new WSJoinAcceptPacket { userName = joinPacket.userName }), DeliveryMethod.ReliableOrdered);
        }


        private void OnDestroy() {
            netManager.Stop();
        }
    }
}
