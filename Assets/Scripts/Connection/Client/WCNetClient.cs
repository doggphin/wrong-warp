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
    [RequireComponent(typeof(WCPacketCacheManager))]
    [RequireComponent(typeof(WCRollbackManager))]
    public class WCNetClient : BaseSingleton<WCNetClient>, ITicker, INetEventListener {
        private WCEntityManager entityManager;
        private NetPeer serverPeer;
        private NetDataWriter writer = new();
        private string userName = "";
        private bool isJoined = false;
        /// <summary> If this is set to a value, search for a player entity with this ID. </summary>
        private int? myEntityId = null;
        public static WCEntity PlayerEntity {get; private set;} = null;
        public static AbstractPlayer Player {get; private set;} = null;

        private static WWatch watch;
        // If client is sending stuff too late (ping is better than they're pretending it is), lower window + skip a couple ticks
        // If client is sending stuff too early (ping is worse than they're pretending it is), increase window + wait a couple ticks
        // This should be done by temporarily increasing the watch AdvanceTick speed.
        //private static int TickOffsetWindow = WCommon.TICKS_PER_SNAPSHOT + 1;
        public static int CentralTimingTick { get; set; }
        private static int windowSize = WCommon.TICKS_PER_SNAPSHOT * 2;
        //private int DesiredTickOffset = -TickOffsetWindow * 2; // Initially want to start in the future
        public static int ObservingTick => CentralTimingTick - windowSize;
        public static int SendingTick => CentralTimingTick + windowSize;
        public int GetTick() => SendingTick;
        public float GetPercentageThroughTick() => watch.GetPercentageThroughTick();
        public float GetPercentageThroughTickCurrentFrame() => percentageThroughTickCurrentFrame;
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
            watch = new();

            ControlsManager.Activate();

            isActivated = true;

            ChatUiManager.SendChatMessage += SendChatMessage;
            NetPacketForClient<WSDefaultControllerStatePkt>.Apply += HandleDefaultControllerState;
            NetPacketForClient<WSEntitiesLoadedDeltaPkt>.Apply += HandleEntitiesLoadedDelta;
            NetPacketForClient<WSSetPlayerEntityPkt>.ApplyUnticked += HandleSetPlayerEntity;
            NetPacketForClient<WSJoinAcceptPkt>.ApplyUnticked += HandleJoinAccept;
        }

        public Action<WDisconnectInfo> Disconnected;
        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo) => WNetManager.Disconnect(new WDisconnectInfo { reason = disconnectInfo.Reason.ToString(), wasExpected = false});

        protected override void OnDestroy() {
            ChatUiManager.SendChatMessage -= SendChatMessage;
            NetPacketForClient<WSDefaultControllerStatePkt>.Apply -= HandleDefaultControllerState;
            NetPacketForClient<WSEntitiesLoadedDeltaPkt>.Apply -= HandleEntitiesLoadedDelta;
            NetPacketForClient<WSSetPlayerEntityPkt>.ApplyUnticked -= HandleSetPlayerEntity;
            NetPacketForClient<WSJoinAcceptPkt>.ApplyUnticked -= HandleJoinAccept;
            base.OnDestroy();
        }

        private float percentageThroughTickCurrentFrame;
        void Update() {
            percentageThroughTickCurrentFrame = GetPercentageThroughTick();
            if(isActivated && GetPercentageThroughTick() > 1) {
                watch.AdvanceTick();
                AdvanceTick(true);
            }
        }

        private int mostRecentlySentTick = 0;
        public void AdvanceTick(bool allowTickCompensation = false) {
            void GetPlayerReference() {
                if(PlayerEntity != null || myEntityId == null)
                    return;

                WCEntity entity = WCEntityManager.GetEntityById(myEntityId.Value);

                Debug.Log($"Entity is {entity}!");
                if(entity == null)
                    return;

                myEntityId = null;
                PlayerEntity = entity;
                Player = PlayerEntity.GetComponent<AbstractPlayer>();
                Player.EnablePlayer();
                ControlsManager.SetPlayer(Player);
            }

            if(!isJoined)
                return;

            // Only check for tick compensation if speedup/slowdown is not already being done
            if(CentralTimingTick > mostRecentlySentTick) {
                CheckForTickCompensation();
            }

            WCPacketCacheManager.ApplyTick(ObservingTick);

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
                if(tickDifferenceTracker.ReadingsCount > 10) {
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
        

        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod) {
            int tick = reader.GetInt();
            tickDifferenceTracker.AddTickDifferenceReading(tick - ObservingTick, windowSize);
            WCPacketForClientUnpacker.ConsumeAllPackets(tick, reader);
            //WPacketCommunication.ReadMultiPacket(peer, reader, ProcessPacketFromReader);  <-- old way
        }

        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType) => throw new NotImplementedException();
        public void OnConnectionRequest(ConnectionRequest request) => request.Reject();
        public void OnNetworkError(IPEndPoint endPoint, SocketError socketError) => Debug.Log($"Socket error: {socketError}");
        public void OnNetworkLatencyUpdate(NetPeer peer, int latency) { }
        
        private void HandleSetPlayerEntity(WSSetPlayerEntityPkt pkt) {
            Instance.myEntityId = pkt.entityId;
            Debug.Log($"Setting entity id to {pkt.entityId}!");
        } 

        private void HandleJoinAccept(WSJoinAcceptPkt pkt) { 
            CentralTimingTick = pkt.tick;
            Debug.Log($"Being told to start at tick {pkt.tick}!");

            Instance.isJoined = true;
        }

        
        private void HandleEntitiesLoadedDelta(int tick, WSEntitiesLoadedDeltaPkt entitiesLoadedDelta) {
            foreach(var entityId in entitiesLoadedDelta.entityIdsToRemove) {
                WCPacketCacheManager.CachePacket(
                    tick,
                    new WSEntityKillPkt() {
                        entityId = entityId,
                        reason = WEntityKillReason.Unload,
                    }
                );
            }

            foreach(var entity in entitiesLoadedDelta.entitiesToAdd) {
                WCPacketCacheManager.CachePacket(
                    tick,
                    new WSEntitySpawnPkt() {
                        entity = entity,
                        reason = WEntitySpawnReason.Load
                    }
                );
            }
        }


        private void HandleDefaultControllerState(int receivedTick, WSDefaultControllerStatePkt confirmedControllerState) {
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

        private void ResimulateTicks(int fromTick) {
            int tickDifference = SendingTick - fromTick - 1;

            CentralTimingTick -= tickDifference;

            for(int i=0; i<tickDifference; i++) {
                AdvanceTick(false);
            }
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
