using LiteNetLib;
using LiteNetLib.Utils;
using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

using Networking.Shared;
using Controllers.Shared;
using System.Collections.Generic;

namespace Networking.Client {
    [RequireComponent(typeof(WCEntityManager))]
    public class WCNetClient : BaseSingleton<WCNetClient>, INetEventListener {
        private WCEntityManager entityManager;
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

        
        public void OnPeerConnected(NetPeer peer) {
            Debug.Log("Connected to server: " + peer.Address);
            serverPeer = peer;

            Activate();

            WCJoinRequestPkt joinRequest = new() { userName = userName };
            WPacketCommunication.SendSingle(writer, serverPeer, CentralTimingTick, joinRequest, DeliveryMethod.ReliableOrdered);
        }
        private bool isActivated;
        private void Activate() {
            if(isActivated)
                return;
            
            DontDestroyOnLoad(gameObject);
            entityManager = GetComponent<WCEntityManager>();
            userName = $"{Environment.MachineName}_{new System.Random().Next(1000000)}";
            packetCacher = new();
            watch = new();

            ControlsManager.Activate();
            ChatUiManager.SendChatMessage += SendChatMessage;

            isActivated = true;
        }

        public Action<WDisconnectInfo> Disconnected;
        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo) {
            WNetManager.Disconnect(new WDisconnectInfo { reason = disconnectInfo.Reason.ToString(), wasExpected = false});
        }
        protected override void OnDestroy() {
            base.OnDestroy();
            ChatUiManager.SendChatMessage -= SendChatMessage;
            
            Destroy(gameObject);
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

        // Update this when general updates are actually implemented, if ever
        public bool ConsumeGeneralUpdate(int tick, WPacketType packetType, NetDataReader reader) {
            switch(packetType) {
                default:
                    Debug.Log($"Unrecognized general update packet type {packetType}!");
                    return false;
            }
        }


        private static readonly Dictionary<WPacketType, Action<int, int, NetDataReader>> entityPacketHandlers = new() {
            { WPacketType.SEntityTransformUpdate, HandleEntityTransformUpdate }
        };
        public bool ConsumeEntityUpdate(int tick, int entityId, WPacketType packetType, NetDataReader reader) {
            if(!entityPacketHandlers.TryGetValue(packetType, out Action<int, int, NetDataReader> handler)) {
                Debug.Log($"Received an (unimplemented) {(ushort)packetType} entity update packet!");
                return false;
            }

            handler.Invoke(tick, entityId, reader);
            return true;
        }


        private static readonly Dictionary<WPacketType, Action<int, NetDataReader>> packetHandlers = new() {
            { WPacketType.SJoinAccept, HandleJoinAccept },
            { WPacketType.SChunkDeltaSnapshot, HandleChunkUpdateSnapshot },
            { WPacketType.SEntitiesLoadedDelta, HandleEntitiesLoadedDelta },
            { WPacketType.SDefaultControllerState, HandleDefaultControllerState },
            { WPacketType.SChunkReliableUpdates, HandleChunkReliableUpdates },
            { WPacketType.SChatMessage, HandleChatMessage },
            { WPacketType.SFullEntitiesSnapshot, HandleFullEntitiesSnapshot },
            { WPacketType.SEntitySpawn, HandleEntitySpawn },
            { WPacketType.SEntityKill, HandleEntityKill },
        };
        public bool ProcessPacketFromReader(NetPeer peer, NetDataReader reader, int tick, WPacketType packetType) {
            if(!packetHandlers.TryGetValue(packetType, out Action<int, NetDataReader> handler)) {
                Debug.LogError($"No handler for {(ushort)packetType} packets!");
                return false;
            }

            Debug.Log($"Processing {packetType}!");
            tickDifferenceTracker.AddTickDifferenceReading(tick - ObservingTick, windowSize);
            handler.Invoke(tick, reader);
            return true;
        }


        private static void HandleJoinAccept(int tick, NetDataReader reader) {
            WSJoinAcceptPkt pkt = new();
            pkt.Deserialize(reader);
            Instance.playerEntityIdToFind = pkt.playerEntityId;
    
            CentralTimingTick = pkt.tick;
            Debug.Log($"Being told to start at tick {pkt.tick}!");

            Instance.isJoined = true;
        }
        private static void HandleChunkUpdateSnapshot(int tick, NetDataReader reader) {
            WSChunkDeltaSnapshotPkt chunkSnapshotPkt = new() {
                c_startTick = tick - WCommon.TICKS_PER_SNAPSHOT
            };
            chunkSnapshotPkt.Deserialize(reader);
        }
        private static void HandleEntitiesLoadedDelta(int tick, NetDataReader reader) {
            WSEntitiesLoadedDeltaPkt entitiesLoadedDelta = new();
            entitiesLoadedDelta.Deserialize(reader);

            foreach(var entityId in entitiesLoadedDelta.entityIdsToRemove) {
                Instance.packetCacher.CachePacket(
                    tick,
                    new WSEntityKillPkt() {
                        entityId = entityId,
                        reason = WEntityKillReason.Unload,
                    }
                );
            }

            foreach(var entity in entitiesLoadedDelta.entitiesToAdd) {
                Instance.packetCacher.CachePacket(
                    tick,
                    new WSEntitySpawnPkt() {
                        entity = entity,
                        reason = WEntitySpawnReason.Load
                    }
                );
            }
        }
        private static void HandleEntityTransformUpdate(int receivedTick, int entityId, NetDataReader reader) {
            if(receivedTick < CentralTimingTick - WCommon.TICKS_PER_SECOND) {

            }

            WSEntityTransformUpdatePkt entityTransformUpdate = new();
            entityTransformUpdate.Deserialize(reader);
            entityTransformUpdate.entityId = entityId;
            Instance.packetCacher.CachePacket(receivedTick, entityTransformUpdate);
        }
        private static void HandleDefaultControllerState(int receivedTick, NetDataReader reader) {
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
            Instance.ResimulateTicks(receivedTick);

            // Reset the player's rotation to before the rollback
            Player.SetRotation(originalRotation);
        }
        private static void HandleChatMessage(int receivedTick, NetDataReader reader) {
            WSChatMessagePkt pkt = new();
            pkt.Deserialize(reader);
            Instance.packetCacher.CachePacket(receivedTick, pkt);
        }
        private static void HandleChunkReliableUpdates(int receivedTick, NetDataReader reader) {
            WSChunkReliableUpdatesPkt pkt = new();
            pkt.Deserialize(reader);
        }
        private static void HandleFullEntitiesSnapshot(int receivedTick, NetDataReader reader) {
            WSFullEntitiesSnapshotPkt pkt = new();
            pkt.Deserialize(reader);
            WCEntityManager.HandleFullEntitiesSnapshot(pkt);
        }
        private static void HandleEntityKill(int receivedTick, NetDataReader reader) {
            WSEntityKillPkt pkt = new();
            pkt.Deserialize(reader);
            WCEntityManager.KillEntity(pkt);
        }
        private static void HandleEntitySpawn(int receivedTick, NetDataReader reader) {
            WSEntitySpawnPkt pkt = new();
            pkt.Deserialize(reader);
            WCEntityManager.Spawn(pkt);
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
