using LiteNetLib;
using UnityEngine;

using Code.Shared;
using LiteNetLib.Utils;
using System.Net;
using System.Net.Sockets;

namespace Code.Server {
    public class WNetServer : MonoBehaviour, INetEventListener {
        private NetManager netManager;

        private NetDataWriter writer;
        private NetPacketProcessor packetProcessor;

        public int Tick { get; private set; }

        private void Awake() {
            DontDestroyOnLoad(gameObject);
            packetProcessor = new();
            writer = new();

            packetProcessor.SubscribeReusable<WCJoinPacket, NetPeer>(OnJoinReceived);
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


        private NetDataWriter WriteSerializable<T>(WPacketType packetType, T packet) where T : class, INetSerializable {
            writer.Reset();
            writer.Put((ushort)packetType);
            packet.Serialize(writer);
            return writer;
        }


        public void OnConnectionRequest(ConnectionRequest request) {
            Debug.Log("Received a connection request!");
            request.AcceptIfKey("WW 0.01");
        }
        public void OnNetworkError(IPEndPoint endPoint, SocketError socketError) => throw new System.NotImplementedException();
        public void OnNetworkLatencyUpdate(NetPeer peer, int latency) => throw new System.NotImplementedException();


        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod) {
            Debug.Log("Received data!");
            packetProcessor.ReadAllPackets(reader, peer);
            reader.Recycle();
        }


        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType) => throw new System.NotImplementedException();
        public void OnPeerConnected(NetPeer peer) {
            Debug.Log($"Player connected: {peer.Address}!");
        }
        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo) {
            Debug.Log($"Player disconnected: {disconnectInfo.Reason}!");
        }
    

        private void OnJoinReceived(WCJoinPacket joinPacket, NetPeer peer) {
            Debug.Log($"Join packet received for {joinPacket.userName}");

            peer.Send(WriteSerializable(WPacketType.SJoinAccept, new WSJoinAcceptPacket { userName = joinPacket.userName }), DeliveryMethod.ReliableOrdered);
        }


        private void OnDestroy() {
            netManager.Stop();
        }
    }
}
