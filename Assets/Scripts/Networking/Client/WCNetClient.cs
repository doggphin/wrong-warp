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
        public static WCEntity PlayerEntity {get; private set;} = null;
        public static IPlayer Player {get; private set;} = null;

        private static WWatch watch;
        public static float PercentageThroughTick => watch.GetPercentageThroughTick();
        public static int Tick {get; set;}
        // Later, server should be able to tell if a client is overcompensating or undercompensating,
        // And either:
        // If client is sending stuff too late (ping is better than they're pretending it is), lower window + skip a couple ticks
        // If client is sending stuff too early (ping is worse than they're pretending it is), increase window + wait a couple ticks
        // This should be done by temporarily increasing the watch AdvanceTick speed.
        //private static int TickOffsetWindow = WCommon.TICKS_PER_SNAPSHOT + 1;
        private static int windowSize = WCommon.TICKS_PER_SNAPSHOT * 2;
        //private int DesiredTickOffset = -TickOffsetWindow * 2; // Initially want to start in the future
        public static int ObservingTick => Tick - windowSize;
        public static int SendingTick => Tick + windowSize;
        private WCTickDifferenceTracker tickDifferenceTracker = new();
        private int necessaryTickCompensation = 0;

        public static WCNetClient Instance { get; private set; }
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

            WCTimedPacketSlotter.Init();
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

            Tick += 1;

            CheckForTickCompensation();

            //WCEntityManager.SetEntitiesToTick(Tick);
            WCTimedPacketSlotter.ApplySlottedPacketsFromTick(ObservingTick);

            // If i'm too far ahead, skip this tick
            if(necessaryTickCompensation < 0) {
                Debug.Log("Slowing down a tick!");
                necessaryTickCompensation += 1;
                Tick -= 1;
                return;
            }

            TryFindPlayer();
            ControlsManager.PollAndControl(SendingTick);

            WPacketCommunication.SendSingle(writer, server, SendingTick,
                new WCGroupedInputsPkt() { inputsSerialized = new WInputsSerializable[]{ ControlsManager.inputs[SendingTick] } },
                DeliveryMethod.Unreliable);

            if(necessaryTickCompensation > 0) {
                Debug.Log($"Speeding up a tick on sending tick tick {SendingTick}!");
                necessaryTickCompensation -= 1;
                AdvanceTick();
            }
        }
        private void CheckForTickCompensation() {
            if(necessaryTickCompensation == 0) {
                if(tickDifferenceTracker.ReadingsCount > 60) {
                    int requestedDifference = (int)Mathf.Round(tickDifferenceTracker.GetRequiredCompensation());

                    if(Math.Abs(requestedDifference) > 1) {
                        necessaryTickCompensation = requestedDifference;
                        print($"Setting tick compensation to {necessaryTickCompensation}");
                    }

                    tickDifferenceTracker.ClearTickDifferencesBuffer();
                }
            } else {
                tickDifferenceTracker.ClearTickDifferencesBuffer();
            }  
        }
        private void TryFindPlayer() {
            if(PlayerEntity == null && myEntityId != null) {
                WCEntity entity = WCEntityManager.GetEntityById(myEntityId.Value);

                if(entity != null) {
                    myEntityId = null;
                    PlayerEntity = entity;
                    Player = PlayerEntity.GetComponent<IPlayer>();
                    Player.InitAsControllable();
                    Player.EnablePlayer();
                    ControlsManager.player = Player;
                    entity.isMyPlayer = true;
                }
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

            switch(packetType) {
                case WPacketType.SEntityTransformUpdate:
                    HandleEntityTransformUpdate(tick, entityId, reader);
                    return true;
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
            
            tickDifferenceTracker.AddTickDifferenceReading(tick - ObservingTick, windowSize);

            switch (packetType) {
                case WPacketType.SJoinAccept:
                    HandleJoinAccept(reader);
                    return true;          
                case WPacketType.SChunkDeltaSnapshot:
                    HandleChunkUpdateSnapshot(tick, reader);
                    return true;
                case WPacketType.SEntitiesLoadedDelta:
                    HandleEntitiesLoadedDelta(tick, reader);
                    return true;
                case WPacketType.SDefaultControllerState:
                    HandleDefaultControllerState(tick, reader);
                    return true;
                default: {
                    Debug.Log($"Received an (unimplemented) {packetType} packet!");
                    return false;
                }
            }
        }


        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod) {
            WPacketCommunication.ReadMultiPacket(peer, reader, ProcessPacketFromReader, true);
        }


        public void OnPeerConnected(NetPeer peer) {
            Debug.Log("Connected to server: " + peer.Address);
            server = peer;

            WCJoinRequestPkt joinRequest = new() { userName = userName };
            
            Debug.Log($"Sending join packet with username {joinRequest.userName}");

            WPacketCommunication.SendSingle(writer, server, Tick, joinRequest, DeliveryMethod.ReliableOrdered);
        }


        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType) => throw new NotImplementedException();


        public void OnConnectionRequest(ConnectionRequest request) { request.Reject(); }


        public void OnNetworkError(IPEndPoint endPoint, SocketError socketError) { Debug.Log($"Socket error: {socketError}"); }


        public void OnNetworkLatencyUpdate(NetPeer peer, int latency) { Ping = latency; }


        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo) { onDisconnected(disconnectInfo); }


        private void HandleChunkUpdateSnapshot(int tick, NetDataReader reader) {
            WSChunkDeltaSnapshotPkt chunkSnapshotPkt = new() {
                startTick = tick,
                c_entityHandler = ConsumeEntityUpdate,
                c_generalHandler = ConsumeGeneralUpdate
            };
            chunkSnapshotPkt.Deserialize(reader);
        }


        private void HandleEntitiesLoadedDelta(int tick, NetDataReader reader) {
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
        }


        private void HandleJoinAccept(NetDataReader reader) {
            WSJoinAcceptPkt pkt = new();
            pkt.Deserialize(reader);
            myEntityId = pkt.playerEntityId;

            Tick = pkt.tick;
            Debug.Log($"Being told to start at tick {pkt.tick}!");

            isJoined = true;
        }


        private void HandleEntityTransformUpdate(int tick, int entityId, NetDataReader reader) {
            WSEntityTransformUpdatePkt entityTransformUpdate = new();
            entityTransformUpdate.Deserialize(reader);
            entityTransformUpdate.entityId = entityId;
            WCTimedPacketSlotter.SlotPacket(tick, entityTransformUpdate);
        }

        private void HandleDefaultControllerState(int tick, NetDataReader reader) {
            WSDefaultControllerStatePkt confirmedControllerState = new();
            confirmedControllerState.Deserialize(reader);

            //Debug.Log($"Received a default controller state for tick {tick}! Observing tick is {ObservingTick}! Sending tick is {SendingTick}");
            bool isInSync = WCRollbackManager.ReceiveDefaultControllerStateConfirmation(tick, confirmedControllerState);
            if(isInSync)
                return;

            //Debug.Log($"You're out of sync! Running it back from {tick}.");
            WCRollbackManager.Rollback(SendingTick, tick, confirmedControllerState);

            int tickDifference = SendingTick - tick;

            Tick -= tickDifference;
            necessaryTickCompensation += tickDifference;

            AdvanceTick();
        }
    }
}
