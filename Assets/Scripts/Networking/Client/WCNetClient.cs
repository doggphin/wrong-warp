using LiteNetLib;
using LiteNetLib.Utils;
using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

using Networking.Shared;
using Controllers.Shared;
using Unity.VisualScripting;

namespace Networking.Client {
    public class WCNetClient : MonoBehaviour, INetEventListener {
        private NetPeer server;
        private NetManager netManager;
        private Action<DisconnectInfo> onDisconnected;
        private NetDataWriter writer = new();
        public int Ping { get; private set; }
        private string userName;
        private bool isJoined = false;
        private int? myEntityId = null;
        private WCEntity myEntity = null;
        private IPlayer myPlayer = null;
        private WInputsSerializable[] inputs;

        private static WWatch watch;
        public static float PercentageThroughTick => watch.GetPercentageThroughTick();
        public int Tick {get; private set;}
        // Later, server should be able to tell if a client is overcompensating or undercompensating,
        // And either:
        // If client is sending stuff too late (ping is better than they're pretending it is), lower window + skip a couple ticks
        // If client is sending stuff too early (ping is worse than they're pretending it is), increase window + wait a couple ticks
        // This should be done by temporarily increasing the watch AdvanceTick speed.
        //private static int TickOffsetWindow = WCommon.TICKS_PER_SNAPSHOT + 1;
        private int windowSize = 5;
        //private int DesiredTickOffset = -TickOffsetWindow * 2; // Initially want to start in the future
        public int ObservingTick => Tick - windowSize;
        public int SendingTick => Tick + windowSize;
        private WCTickDifferenceTracker tickDifferenceTracker = new();
        private int necessaryTickCompensation = 0;

        public WCNetClient Instance { get; private set; }
        private void Awake() {
            if(Instance != null)
                Destroy(gameObject);

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void Init() {
            DontDestroyOnLoad(gameObject);
            System.Random rand = new();
            userName = $"{Environment.MachineName}_{rand.Next(1000000)}";
            
            netManager = new NetManager(this) {
                AutoRecycle = true,
                IPv6Enabled = false
            };
            netManager.Start();

            inputs = new WInputsSerializable[WCommon.TICKS_PER_SECOND];
            for(int i=0; i<inputs.Length; i++) {
                inputs[i] = new();
            }

            watch = new();
            watch.Start();
        }


        private void Update() {
            netManager.PollEvents();

            if(PercentageThroughTick > 1) {
                watch.AdvanceTick();
                AdvanceTick();
            }
        }


        public void AdvanceTick() {
            if(!isJoined)
                return;

            Tick++;

            if(necessaryTickCompensation == 0) {
                if(tickDifferenceTracker.ReadingsCount > 60) {
                    int requestedDifference = (int)Mathf.Round(tickDifferenceTracker.GetRequiredCompensation());

                    if(Mathf.Abs(requestedDifference) > 1) {
                        necessaryTickCompensation = requestedDifference;
                        print($"Setting tick compensation to {necessaryTickCompensation}");
                    }

                    tickDifferenceTracker.ClearTickDifferencesBuffer();
                }
            } else {
                tickDifferenceTracker.ClearTickDifferencesBuffer();
            }  

            WCEntityManager.ReadyForNextTick();
            WCTimedPacketSlotter.ApplyTick(ObservingTick);

            // If i'm too far ahead, skip this tick
            if(necessaryTickCompensation < 0) {
                Debug.Log("Slowing down a tick!");
                necessaryTickCompensation += 1;
                Tick--;
                return;
            }

            ControlsManager.Poll(inputs[WCommon.GetModuloTPS(Tick)]);

            WCGroupedInputsPkt inputsToSend = new() {
                inputsSerialized = new WInputsSerializable[] { inputs[WCommon.GetModuloTPS(Tick)] },
            };

            WPacketComms.SendSingle(writer, server, SendingTick, inputsToSend, DeliveryMethod.Unreliable);

            if(myEntity == null && myEntityId != null) {
                
                WCEntity entity = WCEntityManager.GetEntityById(myEntityId.Value);

                if(entity != null) {

                    myEntityId = null;
                    myEntity = entity; 
                    myPlayer = myEntity.GetComponent<IPlayer>();
                    myPlayer.EnablePlayer();
                }
            }

            if(necessaryTickCompensation > 0) {
                Debug.Log("Speeding up a tick!");
                necessaryTickCompensation -= 1;
                AdvanceTick();
            }
        }


        public void Connect(string ip, Action<DisconnectInfo> onDisconnected) {
            this.onDisconnected = onDisconnected;

            Debug.Log($"Connecting to {ip}:{WCommon.WRONGWARP_PORT}");
            netManager.Connect(ip, WCommon.WRONGWARP_PORT, "WW 0.01");
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

            //Debug.Log($"Consuming update for {tick - ObservingTick} ticks in the future (Reading tick is {ObservingTick}, tick received is {tick})");


            switch(packetType) {
                case WPacketType.SEntityTransformUpdate: {
                    WSEntityTransformUpdatePkt entityTransformUpdate = new();
                    entityTransformUpdate.Deserialize(reader);
                    entityTransformUpdate.entityId = entityId;
                    WCTimedPacketSlotter.SlotPacket(tick, entityTransformUpdate);
                    return true;
                }

                default: {
                    Debug.Log($"Unrecognized entity update packet type {packetType}!");
                    break;
                }
            }

            return false;
        }


        private bool ProcessPacketFromReader(
            NetPeer peer,
            NetDataReader reader,
            int tick,
            WPacketType packetType) {
            
            tickDifferenceTracker.AddTickDifferenceReading(tick - ObservingTick, windowSize);

            switch (packetType) {
                case WPacketType.SJoinAccept: {
                    WSJoinAcceptPkt pkt = new();
                    pkt.Deserialize(reader);
                    myEntityId = pkt.playerEntityId;

                    this.Tick = pkt.tick;
                    Debug.Log($"Being told to start at tick {pkt.tick}!");

                    isJoined = true;
                    return true;
                }
                    
                case WPacketType.SChunkDeltaSnapshot: {
                    WSChunkDeltaSnapshotPkt chunkSnapshotPkt = new() {
                        startTick = tick,
                        c_entityHandler = ConsumeEntityUpdate,
                        c_generalHandler = ConsumeGeneralUpdate
                    };
                    chunkSnapshotPkt.Deserialize(reader);

                    return true;
                }

                case WPacketType.SEntitiesLoadedDelta: {
                    WSEntitiesLoadedDeltaPkt entitiesLoadedDelta = new();
                    entitiesLoadedDelta.Deserialize(reader);

                    foreach(var entityId in entitiesLoadedDelta.entityIdsToRemove) {
                        WCTimedPacketSlotter.SlotPacket(
                            tick,
                            new WSEntityKillPkt() {
                                entityId = entityId,
                                reason = WEntityKillReason.Unload,
                            }
                        );
                    }

                    foreach(var entity in entitiesLoadedDelta.entitiesToAdd) {
                        WCTimedPacketSlotter.SlotPacket(
                            tick,
                            new WSEntitySpawnPkt() {
                                entity = entity,
                                reason = WEntitySpawnReason.Load
                            }
                        );
                    }

                    return true;
                }

                default: {
                    Debug.Log($"Received an (unimplemented) {packetType} packet!");
                    return false;
                }
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
