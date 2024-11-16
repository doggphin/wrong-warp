using LiteNetLib;
using LiteNetLib.Utils;
using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

using Networking.Shared;

namespace Networking.Client {
    public class WNetClient : MonoBehaviour, INetEventListener {
        private NetPeer server;
        private NetManager netManager;

        private NetDataWriter writer;
        private Action<DisconnectInfo> onDisconnected;

        private string userName;
        public int Ping { get; private set; }

        private void Awake() {
            DontDestroyOnLoad(gameObject);
            writer = new();
            System.Random rand = new();
            userName = $"{Environment.MachineName}_{rand.Next(1000000)}";

            netManager = new NetManager(this) {
                AutoRecycle = true,
                IPv6Enabled = false
            };
            netManager.Start();
        }


        public void Connect(string ip, Action<DisconnectInfo> onDisconnected) {
            this.onDisconnected = onDisconnected;

            Debug.Log($"Connecting to {ip}:{WNetCommon.WRONGWARP_PORT}");
            netManager.Connect(ip, WNetCommon.WRONGWARP_PORT, "WW 0.01");
        }


        private void Update() {
            netManager.PollEvents();
        }


        public void SendPacket<T>(WPacketType packetType, T packet, DeliveryMethod deliveryMethod) where T : INetSerializable, new() {
            if (server == null)
                return;
            writer.Reset();
            writer.Put((ushort)packetType);
            packet.Serialize(writer);
            
            server.Send(writer, deliveryMethod);
        }


        public void OnPeerConnected(NetPeer peer) {
            Debug.Log("Connected to server: " + peer.Address);
            server = peer;

            WCJoinPacket joinPacket = new() { userName = userName };
            Debug.Log($"Sending join packet with username {joinPacket.userName}");
            SendPacket(WPacketType.CJoin, joinPacket, DeliveryMethod.ReliableOrdered);
        }


        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod) {
            Debug.unityLogger.Log("Received a packet!");
            WPacketType packetType = (WPacketType)reader.GetUShort();

            switch(packetType) {
                default:
                    Debug.Log($"Could not handle packet of type {packetType}!");
                    break;
            }
        }


        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType) => throw new NotImplementedException();


        public void OnConnectionRequest(ConnectionRequest request) { request.Reject(); }


        public void OnNetworkError(IPEndPoint endPoint, SocketError socketError) { Debug.Log($"Socket error: {socketError}"); }


        public void OnNetworkLatencyUpdate(NetPeer peer, int latency) { Ping = latency; }


        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo) { onDisconnected(disconnectInfo); }
    }
}
