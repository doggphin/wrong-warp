using LiteNetLib;
using LiteNetLib.Utils;
using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

using Networking.Shared;
using Controllers.Shared;

namespace Networking.Client {
    public class WCNetClient : MonoBehaviour, INetEventListener {
        private NetPeer server;
        private NetManager netManager;
        private Action<DisconnectInfo> onDisconnected;
        private NetDataWriter writer = new();
        public int Ping { get; private set; }
        private string userName;
        private bool isJoined = false;
        /// <summary> If this is set to a value, search for a player entity with this ID. </summary>
        private int? playerEntityIdToFind = null;
        public static WCEntity PlayerEntity {get; private set;} = null;
        public static IPlayer Player {get; private set;} = null;

        private static WWatch watch;
        public static float PercentageThroughTick => watch.GetPercentageThroughTick();
        // If client is sending stuff too late (ping is better than they're pretending it is), lower window + skip a couple ticks
        // If client is sending stuff too early (ping is worse than they're pretending it is), increase window + wait a couple ticks
        // This should be done by temporarily increasing the watch AdvanceTick speed.
        //private static int TickOffsetWindow = WCommon.TICKS_PER_SNAPSHOT + 1;
        public static int CentralTimingTick {get; set;}
        private static int windowSize = WCommon.TICKS_PER_SNAPSHOT * 2;
        //private int DesiredTickOffset = -TickOffsetWindow * 2; // Initially want to start in the future
        public static int ObservingTick => CentralTimingTick - windowSize;
        public static int SendingTick => CentralTimingTick + windowSize;
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

            WCPacketCacher.Init();
            watch = new();
            watch.Start();

            ChatUiManager.SendChatMessage += SendChatMessage;
        }


        private void Update() {
            netManager.PollEvents();

            if(PercentageThroughTick > 1) {
                watch.AdvanceTick();
                AdvanceTick(true);
            }
        }


        private int mostRecentlySentTick = 0;
        public void AdvanceTick(bool allowTickCompensation = false) {
            if(!isJoined)
                return;

            // Only check for tick compensation if speedup/slowdown is not already being done
            if(CentralTimingTick > mostRecentlySentTick) {
                CheckForTickCompensation();
            }

            WCPacketCacher.ApplyTick(ObservingTick);

            // If client is too far ahead, skip this tick
            if(allowTickCompensation && necessaryTickCompensation < 0) {
                necessaryTickCompensation += 1;
                return;
            }

            // This should be done in a better way...
            TryFindPlayer();
            ControlsManager.PollAndControl(SendingTick);

            // Send inputs to the server
            if(CentralTimingTick > mostRecentlySentTick) {
                //Debug.Log($"Sending inputs for {SendingTick}!");
                WPacketCommunication.SendSingle(
                    writer, 
                    server, 
                    SendingTick,
                    new WCGroupedInputsPkt() { inputsSerialized = new WInputsSerializable[]{ ControlsManager.inputs[SendingTick] } },
                    DeliveryMethod.Unreliable);
                mostRecentlySentTick = CentralTimingTick;
            }
            
            CentralTimingTick += 1;

            // If client is too far ahead, run another tick
            if(allowTickCompensation && necessaryTickCompensation > 0) {
                necessaryTickCompensation -= 1;
                AdvanceTick();
            }
        }


        // Checks most recently received packets. Tries to 
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


        // Tries to initialize a player from the entity with ID myEntityId.
        private void TryFindPlayer() {
            if(PlayerEntity == null && playerEntityIdToFind != null) {
                WCEntity entity = WCEntityManager.GetEntityById(playerEntityIdToFind.Value);

                if(entity != null) {
                    playerEntityIdToFind = null;
                    PlayerEntity = entity;
                    Player = PlayerEntity.GetComponent<IPlayer>();
                    Player.EnablePlayer();
                    ControlsManager.player = Player;
                    entity.isMyPlayer = true;
                }
            }
        }


        public void Connect(string address, ushort port, Action<DisconnectInfo> onDisconnected) {
            this.onDisconnected = onDisconnected;

            Debug.Log($"Attempting to connect to {address}:{port}");
            netManager.Connect(address, port, "WW 0.01");
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


        public bool ConsumeEntityUpdate(
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


        public bool ProcessPacketFromReader(
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
                case WPacketType.SChatMessage:
                    HandleChatMessage(tick, reader);
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

            WPacketCommunication.SendSingle(writer, server, CentralTimingTick, joinRequest, DeliveryMethod.ReliableOrdered);
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
                WCPacketCacher.CachePacket(
                    tick,
                    new WSEntityKillPkt() {
                        entityId = entityId,
                        reason = WEntityKillReason.Unload,
                    }
                );
            }

            foreach(var entity in entitiesLoadedDelta.entitiesToAdd) {
                WCPacketCacher.CachePacket(
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
            playerEntityIdToFind = pkt.playerEntityId;

            CentralTimingTick = pkt.tick;
            Debug.Log($"Being told to start at tick {pkt.tick}!");

            isJoined = true;
        }


        private void HandleEntityTransformUpdate(int receivedTick, int entityId, NetDataReader reader) {
            if(receivedTick < CentralTimingTick - WCommon.TICKS_PER_SECOND) {

            }

            WSEntityTransformUpdatePkt entityTransformUpdate = new();
            entityTransformUpdate.Deserialize(reader);
            entityTransformUpdate.entityId = entityId;
            WCPacketCacher.CachePacket(receivedTick, entityTransformUpdate);
        }


        private void HandleDefaultControllerState(int receivedTick, NetDataReader reader) {
            WSDefaultControllerStatePkt confirmedControllerState = new();
            confirmedControllerState.Deserialize(reader);

            //Debug.Log($"Received a default controller state for tick {tick}! Observing tick is {ObservingTick}! Sending tick is {SendingTick}");
            bool isInSync = WCRollbackManager.ReceiveDefaultControllerStateConfirmation(receivedTick, confirmedControllerState);
            if(isInSync)
                return;

            // Save rotation before rollback to set player back to later
            Vector2 originalRotation = Player.GetRotation();

            // Roll the player back and resimulate ticks from the tick received
            WCRollbackManager.RollbackDefaultController(SendingTick, receivedTick, confirmedControllerState);
            ResimulateTicks(receivedTick);

            // Reset the player's rotation to before the rollback
            Player.SetRotation(originalRotation);
        }


        private void HandleChatMessage(int receivedTick, NetDataReader reader) {
            WSChatMessagePkt pkt = new();
            pkt.Deserialize(reader);
            WCPacketCacher.CachePacket(receivedTick, pkt);
        }


        private void ResimulateTicks(int fromTick) {
            // Subtract one since we don't want to resimulate the received tick
            int tickDifference = SendingTick - fromTick - 1;
            //Debug.Log($"Need to resimulate from {fromTick}... Turning back from {SendingTick} to {SendingTick - tickDifference}");
            CentralTimingTick -= tickDifference;

            for(int i=0; i<tickDifference; i++) {
                //Debug.Log($"Resimulating {SendingTick}");
                AdvanceTick(false);
            }

            //Debug.Log($"Ended on {SendingTick}");
        }


        private void SendChatMessage(string message) {
            NetDataWriter writer = new();
            WCChatMessagePkt pkt = new() {
                message = message
            };

            // Tick is not relevant here but needs to be written regardless
            WPacketCommunication.SendSingle(writer, server, 0, pkt, DeliveryMethod.ReliableOrdered);
            Debug.Log("Sent message to the server!");
        }
    }
}
