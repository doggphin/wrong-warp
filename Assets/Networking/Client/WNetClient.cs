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

        private Action<DisconnectInfo> onDisconnected;

        private string userName;
        private NetDataWriter writer = new();
        public int Ping { get; private set; }
        public int Tick { get; private set; }

        private void Awake() {
            DontDestroyOnLoad(gameObject);
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


        public void OnPeerConnected(NetPeer peer) {
            Debug.Log("Connected to server: " + peer.Address);
            server = peer;

            WCJoinRequestPkt joinRequest = new() { userName = userName };
            Debug.Log($"Sending join packet with username {joinRequest.userName}");
            WNetPacketComms.SendSingle(writer, server, Tick, WPacketType.CJoin, joinRequest, DeliveryMethod.ReliableOrdered);
        }


        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType) => throw new NotImplementedException();


        public void OnConnectionRequest(ConnectionRequest request) { request.Reject(); }


        public void OnNetworkError(IPEndPoint endPoint, SocketError socketError) { Debug.Log($"Socket error: {socketError}"); }


        public void OnNetworkLatencyUpdate(NetPeer peer, int latency) { Ping = latency; }


        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo) { onDisconnected(disconnectInfo); }


        private bool ProcessPacketFromReader(
            NetPeer peer,
            NetDataReader reader,
            int tick,
            WPacketType packetType) {

            switch(packetType) {
                default:
                    Debug.Log($"Could not handle packet of type {packetType}!");
                    return false;
            }
        }


        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod) {
            Debug.unityLogger.Log("Received a packet!");
            WNetPacketComms.ReadMultiPacket(peer, reader, ProcessPacketFromReader, true);
        }
    }
}
