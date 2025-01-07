using LiteNetLib;
using LiteNetLib.Utils;
using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

using Networking.Shared;
using Controllers.Shared;

namespace Networking.Client {
    [RequireComponent(typeof(WCEntityManager))]
    public class WCNetClient : BaseSingleton<WCNetClient>, INetEventListener {
        WCEntityManager entityManager;
        private NetPeer serverPeer;
        private NetDataWriter writer = new();
        private string userName = "";
        private bool isJoined = false;
        /// <summary> If this is set to a value, search for a player entity with this ID. </summary>
        private int? playerEntityIdToFind = null;
        public static WCEntity PlayerEntity {get; private set;} = null;
        public static IPlayer Player {get; private set;} = null;

        private WCPacketCacher packetCacher;
        private static WWatch watch;
        public static float PercentageThroughTick => watch.GetPercentageThroughTick();
        // If client is sending stuff too late (ping is better than they're pretending it is), lower window + skip a couple ticks
        // If client is sending stuff too early (ping is worse than they're pretending it is), increase window + wait a couple ticks
        // This should be done by temporarily increasing the watch AdvanceTick speed.
        //private static int TickOffsetWindow = WCommon.TICKS_PER_SNAPSHOT + 1;
        public static int CentralTimingTick { get; set; }
        private static int windowSize = WCommon.TICKS_PER_SNAPSHOT * 2;
        //private int DesiredTickOffset = -TickOffsetWindow * 2; // Initially want to start in the future
        public static int ObservingTick => CentralTimingTick - windowSize;
        public static int SendingTick => CentralTimingTick + windowSize;
        private WCTickDifferenceTracker tickDifferenceTracker = new();
        private int necessaryTickCompensation = 0;

        protected override void OnDestroy() {
            base.OnDestroy();
            ChatUiManager.SendChatMessage -= SendChatMessage;
            ControlsManager.player = null;
            ControlsManager.Deactivate();
            Destroy(gameObject);
        }
        
        private bool isActivated = false;
        private void Activate() {
            entityManager = GetComponent<WCEntityManager>();

            userName = $"{Environment.MachineName}_{new System.Random().Next(1000000)}";

            packetCacher = new();
            watch = new();

            ControlsManager.Activate();
            ChatUiManager.SendChatMessage += SendChatMessage;

            isActivated = true;
            DontDestroyOnLoad(gameObject);
        }


        public Action<WDisconnectInfo> Disconnected;
        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo) {
            Disconnected?.Invoke(new WDisconnectInfo { reason = nameof(disconnectInfo.Reason), wasExpected = false});
        }

        
        public Action Connected;
        public void OnPeerConnected(NetPeer peer) {
            Debug.Log("Connected to server: " + peer.Address);
            serverPeer = peer;

            Activate();

            WCJoinRequestPkt joinRequest = new() { userName = userName };
            WPacketCommunication.SendSingle(writer, serverPeer, CentralTimingTick, joinRequest, DeliveryMethod.ReliableOrdered);
        }


        void Update() {
            if(isActivated && PercentageThroughTick > 1) {
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

            packetCacher.ApplyTick(ObservingTick);

            // If client is too far ahead, skip this tick
            if(allowTickCompensation && necessaryTickCompensation < 0) {
                necessaryTickCompensation += 1;
                return;
            }

            // This should be done in a better way...
            GetPlayerReference();
            ControlsManager.PollAndControl(SendingTick);

            // Send inputs to the server
            if(CentralTimingTick > mostRecentlySentTick) {
                //Debug.Log($"Sending inputs for {SendingTick}!");
                WPacketCommunication.SendSingle(
                    writer, 
                    serverPeer, 
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
        private void GetPlayerReference() {
            if(PlayerEntity != null || playerEntityIdToFind == null)
                return;

            WCEntity entity = WCEntityManager.GetEntityById(playerEntityIdToFind.Value);

            if(entity == null)
                return;

            playerEntityIdToFind = null;
            PlayerEntity = entity;
            Player = PlayerEntity.GetComponent<IPlayer>();
            Player.EnablePlayer();
            ControlsManager.player = Player;
            entity.isMyPlayer = true;
        }


        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod) {
            WPacketCommunication.ReadMultiPacket(peer, reader, ProcessPacketFromReader);
        }
        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType) => throw new NotImplementedException();
        public void OnConnectionRequest(ConnectionRequest request) => request.Reject();
        public void OnNetworkError(IPEndPoint endPoint, SocketError socketError) => Debug.Log($"Socket error: {socketError}");
        public void OnNetworkLatencyUpdate(NetPeer peer, int latency) { }

        public bool ConsumeGeneralUpdate(
            int tick,
            WPacketType packetType,
            NetDataReader reader) {

            switch(packetType) {
                default:
                    Debug.Log($"Unrecognized general update packet type {packetType}!");
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
                case WPacketType.SChunkReliableUpdates:
                    HandleChunkReliableUpdates(tick, reader);
                    return true;
                case WPacketType.SChatMessage:
                    HandleChatMessage(tick, reader);
                    return true;
                default: {
                    Debug.Log($"Received an (unimplemented) {(ushort)packetType} packet!");
                    return false;
                }
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


        private void HandleChunkUpdateSnapshot(int tick, NetDataReader reader) {
            WSChunkDeltaSnapshotPkt chunkSnapshotPkt = new() {
                c_startTick = tick - WCommon.TICKS_PER_SNAPSHOT
            };
            chunkSnapshotPkt.Deserialize(reader);
        }


        private void HandleEntitiesLoadedDelta(int tick, NetDataReader reader) {
            WSEntitiesLoadedDeltaPkt entitiesLoadedDelta = new();
            entitiesLoadedDelta.Deserialize(reader);

            foreach(var entityId in entitiesLoadedDelta.entityIdsToRemove) {
                packetCacher.CachePacket(
                    tick,
                    new WSEntityKillPkt() {
                        entityId = entityId,
                        reason = WEntityKillReason.Unload,
                    }
                );
            }

            foreach(var entity in entitiesLoadedDelta.entitiesToAdd) {
                packetCacher.CachePacket(
                    tick,
                    new WSEntitySpawnPkt() {
                        entity = entity,
                        reason = WEntitySpawnReason.Load
                    }
                );
            }
        }


        private void HandleEntityTransformUpdate(int receivedTick, int entityId, NetDataReader reader) {
            if(receivedTick < CentralTimingTick - WCommon.TICKS_PER_SECOND) {

            }

            WSEntityTransformUpdatePkt entityTransformUpdate = new();
            entityTransformUpdate.Deserialize(reader);
            entityTransformUpdate.entityId = entityId;
            packetCacher.CachePacket(receivedTick, entityTransformUpdate);
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
            Debug.Log("Got a chat message!");
            WSChatMessagePkt pkt = new();
            pkt.Deserialize(reader);
            packetCacher.CachePacket(receivedTick, pkt);
        }


        private void HandleChunkReliableUpdates(int receivedTick, NetDataReader reader) {
            Debug.Log("Got a reliable updates!");
            WSChunkReliableUpdatesPkt pkt = new();
            pkt.Deserialize(reader);
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
            WPacketCommunication.SendSingle(writer, serverPeer, 0, pkt, DeliveryMethod.ReliableOrdered);
            Debug.Log("Sent message to the server!");
        }
    }
}
