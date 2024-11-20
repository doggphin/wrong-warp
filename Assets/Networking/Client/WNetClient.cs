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
            WNetPacketComms.SendSingle(writer, server, Tick, joinRequest, DeliveryMethod.ReliableOrdered);
        }


        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType) => throw new NotImplementedException();


        public void OnConnectionRequest(ConnectionRequest request) { request.Reject(); }


        public void OnNetworkError(IPEndPoint endPoint, SocketError socketError) { Debug.Log($"Socket error: {socketError}"); }


        public void OnNetworkLatencyUpdate(NetPeer peer, int latency) { Ping = latency; }


        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo) { onDisconnected(disconnectInfo); }


        private bool ConsumeEntityUpdate(
            int tick,
            int entityId,
            WPacketType packetType,
            NetDataReader reader) {

            switch(packetType) {
                case WPacketType.SEntityTransformUpdate:
                    WSEntityTransformUpdatePkt pkt = new WSEntityTransformUpdatePkt();
                    pkt.Deserialize(reader);
                    if (pkt.position != null)
                        Debug.Log($"Move {entityId} to {pkt.position} at tick {tick}.");
                    return true;

                default:
                    Debug.Log($"Unrecognized entity update packet type {packetType}!");
                    return false;
            }
        }

        private bool ConsumeGeneralUpdate(
            int tick,
            WPacketType packetType,
            NetDataReader reader) {


            switch(packetType) {
                default:
                    Debug.Log($"Unrecognized entity update packet type {packetType}!");
                    return false;
            }
        }

        private bool ProcessPacketFromReader(
            NetPeer peer,
            NetDataReader reader,
            int tick,
            WPacketType packetType) {

            switch (packetType) {
                case WPacketType.SChunkSnapshot:
                    WSChunkSnapshotPkt chunkSnapshotPkt = new() {
                        c_headerTick = tick,
                        c_entityHandler = ConsumeEntityUpdate,
                        c_generalHandler = ConsumeGeneralUpdate
                    };
                    chunkSnapshotPkt.Deserialize(reader);
                    return true;

                case WPacketType.SJoinAccept:
                    Debug.Log($"Yippee! I got in the server!!!");
                    return true;

                default:
                    Debug.Log($"Received an (unimplemented) {packetType} packet!");
                    return false;
            }
        }


        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod) {
            WNetPacketComms.ReadMultiPacket(peer, reader, ProcessPacketFromReader, true);
        }
    }
}
