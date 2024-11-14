using LiteNetLib;
using LiteNetLib.Utils;
using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

using Code.Shared;

namespace Code.Client {
    public class WNetClient : MonoBehaviour, INetEventListener {
        private NetPeer server;
        private NetManager netManager;

        private NetDataWriter writer;
        private NetPacketProcessor packetProcessor;
        private Action<DisconnectInfo> onDisconnected;

        private string userName;
        public int Ping { get; private set; }

        private void Awake() {
            DontDestroyOnLoad(gameObject);
            writer = new();
            packetProcessor = new();
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
            netManager.Connect(ip, WNetCommon.WRONGWARP_PORT, "WW 0.01");
        }


        private void Update() {
            netManager.PollEvents();
        }


        public void OnPeerConnected(NetPeer peer) {
            Debug.Log("Connected to server: " + peer.Address);
            server = peer;
        }


        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod) {
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
