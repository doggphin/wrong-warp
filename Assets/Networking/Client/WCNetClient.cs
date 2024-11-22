using LiteNetLib;
using LiteNetLib.Utils;
using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

using Networking.Shared;

namespace Networking.Client {
    public class WCNetClient : MonoBehaviour, INetEventListener {
        private NetPeer server;
        private NetManager netManager;

        private Action<DisconnectInfo> onDisconnected;

        private string userName;
        private NetDataWriter writer = new();
        public int Ping { get; private set; }
        public int Tick { get; private set; }

        private bool isJoined = false;

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


        private void FixedUpdate() {
            if(!isJoined)
                return;

            Tick++;

            WCTimedPacketSlotter.AdvanceTick();
        }


        public void Connect(string ip, Action<DisconnectInfo> onDisconnected) {
            this.onDisconnected = onDisconnected;

            Debug.Log($"Connecting to {ip}:{WCommon.WRONGWARP_PORT}");
            netManager.Connect(ip, WCommon.WRONGWARP_PORT, "WW 0.01");
        }


        private void Update() {
            netManager.PollEvents();
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


        private bool ConsumeEntityUpdate(
            int tick,
            int entityId,
            WPacketType packetType,
            NetDataReader reader) {

            WCPacketSlots slots = WCTimedPacketSlotter.GetPacketSlots(tick);

            switch(packetType) {
                case WPacketType.SEntityTransformUpdate:
                    WSEntityTransformUpdatePkt entityTransformUpdate = new();
                    entityTransformUpdate.Deserialize(reader);
                    entityTransformUpdate.entityId = entityId;
                    slots.entityTransformUpdatePackets.Add(entityTransformUpdate);
                    return true;

                default:
                    Debug.Log($"Unrecognized entity update packet type {packetType}!");
                    break;
            }

            return false;
        }


        private bool ProcessPacketFromReader(
            NetPeer peer,
            NetDataReader reader,
            int tick,
            WPacketType packetType) {
            
            WCPacketSlots slots = WCTimedPacketSlotter.GetPacketSlots(tick);

            switch (packetType) {
                case WPacketType.SJoinAccept:
                    WSJoinAcceptPkt pkt = new();
                    pkt.Deserialize(reader);
                    Debug.Log($"Yippee! I got in the server! My name is {pkt.userName}, and I'm starting at tick {pkt.tick}");
                    Tick = pkt.tick;
                    WCTimedPacketSlotter.Init(Tick);
                    isJoined = true;

                    return true;

                case WPacketType.SChunkDeltaSnapshot:
                    WSChunkDeltaSnapshotPkt chunkSnapshotPkt = new() {
                        c_headerTick = tick,
                        c_entityHandler = ConsumeEntityUpdate,
                        c_generalHandler = ConsumeGeneralUpdate
                    };
                    chunkSnapshotPkt.Deserialize(reader);
                    return true;

                case WPacketType.SEntitiesLoadedDelta:
                    Debug.Log("Received a chunk delta snapshot!");
                    WSEntitiesLoadedDeltaPkt entitiesLoadedDelta = new();
                    entitiesLoadedDelta.Deserialize(reader);
                    foreach(var entityId in entitiesLoadedDelta.entityIdsToRemove) {
                        slots.entityKillPackets.Add(new WSEntityKillPkt() {
                            entityId = entityId,
                            reason = WEntityKillReason.Unload,
                        });
                    }
                    foreach(var entity in entitiesLoadedDelta.entitiesToAdd) {
                        slots.entitySpawnPackets.Add(new WSEntitySpawnPkt() {
                            entity = entity,
                            reason = WEntitySpawnReason.Load
                        });
                    }
                    return true;

                default:
                    Debug.Log($"Received an (unimplemented) {packetType} packet!");
                    return false;
            }
        }


        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod) {
            WPacketComms.ReadMultiPacket(peer, reader, ProcessPacketFromReader, true);
        }


        public void OnPeerConnected(NetPeer peer) {
            Debug.Log("Connected to server: " + peer.Address);
            server = peer;

            WCJoinRequestPkt joinRequest = new() { userName = userName };
            
            Debug.Log($"Sending join packet with username {joinRequest.userName}");

            WPacketComms.SendSingle(writer, server, Tick, joinRequest, DeliveryMethod.ReliableOrdered);
        }


        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType) => throw new NotImplementedException();


        public void OnConnectionRequest(ConnectionRequest request) { request.Reject(); }


        public void OnNetworkError(IPEndPoint endPoint, SocketError socketError) { Debug.Log($"Socket error: {socketError}"); }


        public void OnNetworkLatencyUpdate(NetPeer peer, int latency) { Ping = latency; }


        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo) { onDisconnected(disconnectInfo); }
    }
}
