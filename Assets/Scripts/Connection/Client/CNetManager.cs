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
    [RequireComponent(typeof(CEntityManager))]
    [RequireComponent(typeof(PacketCacheManager))]
    [RequireComponent(typeof(CRollbackManager))]
    [RequireComponent(typeof(CPacketUnpacker))]
    [RequireComponent(typeof(ControlsManager))]
    [RequireComponent(typeof(CPacketPacker))]
    [RequireComponent(typeof(CPacketDefragmenter))]
    public class CNetManager : BaseSingleton<CNetManager>, ITicker, INetEventListener {
        private ControlsManager controlsManager;
        private CPacketPacker packetPacker;
        private CPacketDefragmenter packetDefragmenter;

        private NetPeer serverPeer;
        private NetDataWriter writer = new();
        private string userName = "";
        private bool isJoined = false;
        /// <summary> If this is set to a value, search for a player entity with this ID. </summary>
        private int? myEntityId = null;
        public static CEntity PlayerEntity {get; private set;} = null;
        public static AbstractPlayer Player {get; private set;} = null;

        private static WWatch watch;
        // If client is sending stuff too late (ping is better than they're pretending it is), lower window + skip a couple ticks
        // If client is sending stuff too early (ping is worse than they're pretending it is), increase window + wait a couple ticks
        // This should be done by temporarily increasing the watch AdvanceTick speed.
        //private static int TickOffsetWindow = WCommon.TICKS_PER_SNAPSHOT + 1;
        public static int CentralTimingTick { get; set; }
        private static int windowSize = NetCommon.TICKS_PER_SNAPSHOT * 2;
        //private int DesiredTickOffset = -TickOffsetWindow * 2; // Initially want to start in the future
        public static int ObservingTick => CentralTimingTick - windowSize;
        public static int SendingTick => CentralTimingTick + windowSize;
        public int GetTick() => SendingTick;
        public float GetPercentageThroughTick() => watch.GetPercentageThroughTick();
        public float GetPercentageThroughTickCurrentFrame() => percentageThroughTickCurrentFrame;
        private CTickDifferenceTracker tickDifferenceTracker = new();
        private int necessaryTickCompensation = 0;

        
        public void OnPeerConnected(NetPeer peer) {
            Debug.Log("Connected to server: " + peer.Address);
            serverPeer = peer;

            Activate();

            CJoinRequestPkt joinRequest = new() { userName = userName };
            packetPacker.SendSingleReliable(writer, serverPeer, CentralTimingTick, joinRequest);
        }
        private bool isActivated;
        private void Activate() {
            if(isActivated)
                return;
            
            userName = $"{Environment.MachineName}_{new System.Random().Next(1000000)}";
            watch = new();

            controlsManager = GetComponent<ControlsManager>();
            ControlsManager.ActivateControls();
            packetPacker = GetComponent<CPacketPacker>();
            packetDefragmenter = GetComponent<CPacketDefragmenter>();

            isActivated = true;

            ChatUiManager.SendChatMessage += SendChatMessage;
            SPacket<SDefaultControllerStatePkt>.Apply += HandleDefaultControllerState;
            SPacket<SEntitiesLoadedDeltaPkt>.Apply += HandleEntitiesLoadedDelta;
            SPacket<SSetPlayerEntityPkt>.ApplyUnticked += HandleSetPlayerEntity;
            SPacket<SJoinAcceptPkt>.ApplyUnticked += HandleJoinAccept;
        }

        public Action<WDisconnectInfo> Disconnected;
        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo) => WWNetManager.Disconnect(new WDisconnectInfo { reason = disconnectInfo.Reason.ToString(), wasExpected = false});

        protected override void OnDestroy() {
            ChatUiManager.SendChatMessage -= SendChatMessage;
            SPacket<SDefaultControllerStatePkt>.Apply -= HandleDefaultControllerState;
            SPacket<SEntitiesLoadedDeltaPkt>.Apply -= HandleEntitiesLoadedDelta;
            SPacket<SSetPlayerEntityPkt>.ApplyUnticked -= HandleSetPlayerEntity;
            SPacket<SJoinAcceptPkt>.ApplyUnticked -= HandleJoinAccept;
            base.OnDestroy();
        }

        private float percentageThroughTickCurrentFrame;
        void Update() {
            if(!isActivated)
                return;
            
            percentageThroughTickCurrentFrame = GetPercentageThroughTick();
            if(GetPercentageThroughTick() > 1) {
                watch.AdvanceTick();
                AdvanceTick(true);
            }
        }

        private int mostRecentlySentTick = 0;
        public void AdvanceTick(bool allowTickCompensation = false) {
            // Surely there's a better way of doing this, right?
            // Maybe check for whether playerEntity ID is equal to myEntityId, and if not, then search
            void GetPlayerReference() {
                if(PlayerEntity != null || myEntityId == null)
                    return;

                CEntity entity = CEntityManager.GetEntityById(myEntityId.Value);

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

            PacketCacheManager.ApplyTick(ObservingTick);

            // If client is too far ahead, skip this tick
            if(allowTickCompensation && necessaryTickCompensation < 0) {
                necessaryTickCompensation += 1;
                return;
            }

            // This should be done in a better way...
            GetPlayerReference();
            controlsManager.PollAndControl(SendingTick);

            // Send inputs to the server
            if(CentralTimingTick > mostRecentlySentTick) {
                //Debug.Log($"Sending inputs for {SendingTick}!");
                packetPacker.SendSingleUnreliable(
                    writer, 
                    serverPeer, 
                    SendingTick,
                    new CGroupedInputsPkt() { inputsSerialized = new InputsSerializable[]{ controlsManager.inputs[SendingTick] } }
                );
                mostRecentlySentTick = CentralTimingTick;
            }
            
            CentralTimingTick += 1;

            // If client is too far ahead, run another tick
            if(allowTickCompensation && necessaryTickCompensation > 0) {
                necessaryTickCompensation -= 1;
                AdvanceTick();
            }
        }

        // Checks most recently received packets. Tries to... do something... i should have documented this better
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
            if(deliveryMethod == DeliveryMethod.Unreliable) {
                packetDefragmenter.ProcessUnreliablePacket(reader);
            } else if (deliveryMethod == DeliveryMethod.ReliableOrdered) {
                int tick = reader.GetInt();
                tickDifferenceTracker.AddTickDifferenceReading(tick - ObservingTick, windowSize);
                CPacketUnpacker.ConsumeAllPackets(tick, reader);
            } else {
                Debug.LogError($"Received a packet with unhandled delivery method {deliveryMethod}!");
            }
        }


        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType) => throw new NotImplementedException();
        public void OnConnectionRequest(ConnectionRequest request) => request.Reject();
        public void OnNetworkError(IPEndPoint endPoint, SocketError socketError) => Debug.Log($"Socket error: {socketError}");
        public void OnNetworkLatencyUpdate(NetPeer peer, int latency) { }
        
        private void HandleSetPlayerEntity(SSetPlayerEntityPkt pkt) {
            Instance.myEntityId = pkt.entityId;
            Debug.Log($"Setting entity id to {pkt.entityId}!");
        } 

        private void HandleJoinAccept(SJoinAcceptPkt pkt) { 
            CentralTimingTick = pkt.tick;
            Debug.Log($"Being told to start at tick {pkt.tick}!");

            Instance.isJoined = true;
        }

        
        private void HandleEntitiesLoadedDelta(int tick, SEntitiesLoadedDeltaPkt entitiesLoadedDelta) {
            foreach(var entityId in entitiesLoadedDelta.entityIdsToRemove) {
                PacketCacheManager.CachePacket(
                    tick,
                    new SEntityKillPkt() {
                        entityId = entityId,
                        reason = WEntityKillReason.Unload,
                    }
                );
            }

            foreach(var entity in entitiesLoadedDelta.entitiesToAdd) {
                PacketCacheManager.CachePacket(
                    tick,
                    new SEntitySpawnPkt() {
                        entity = entity,
                        reason = WEntitySpawnReason.Load
                    }
                );
            }
        }


        private void HandleDefaultControllerState(int receivedTick, SDefaultControllerStatePkt confirmedControllerState) {
            bool isInSync = CRollbackManager.ReceiveDefaultControllerStateConfirmation(receivedTick, confirmedControllerState);
            if(isInSync)
                return;

            // Save rotation before rollback to set player back to later
            Vector2 originalRotation = Player.GetRotation();

            // Roll the player back and resimulate ticks from the tick received
            CRollbackManager.RollbackDefaultController(SendingTick, receivedTick, confirmedControllerState);
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
            CChatMessagePkt pkt = new() {
                message = message
            };

            // Tick is not relevant here but something needs to be written there regardless
            packetPacker.SendSingleReliable(writer, serverPeer, SendingTick, pkt);
            Debug.Log("Sent message to the server!");
        }
    }
}
