using LiteNetLib;
using UnityEngine;
using LiteNetLib.Utils;
using System.Net;
using System.Net.Sockets;

using Networking.Shared;
using Controllers.Shared;
using System.Linq;
using System;

namespace Networking.Server {
    [RequireComponent(typeof(WSEntityManager))]
    [RequireComponent(typeof(WSPlayerInputsSlotterManager))]
    [RequireComponent(typeof(WSInventoryManager))]
    public class WSNetServer : BaseSingleton<WSNetServer>, ITicker, INetEventListener {
        private WSEntityManager entityManager;
        private static NetDataWriter writer = new();

        private static int tick;
        public int GetTick() => tick;
        public static int Tick => Instance.GetTick();
        private static WWatch watch;
        private float percentageThroughTickCurrentFrame;
        public float GetPercentageThroughTickCurrentFrame() => percentageThroughTickCurrentFrame;
        public float GetPercentageThroughTick() => watch.GetPercentageThroughTick();
        public static WSPlayer HostPlayer { get; private set; }

        public static Action<WSPlayer> PlayerJoined;
        public static Action<WSPlayer> PlayerLeft;

        private bool isActivated;
        public void Activate() {
            Debug.Log("Activating!");
            if(isActivated)
                return;

            entityManager = GetComponent<WSEntityManager>();
            CreateHostPlayer();
            // Start one second ahead to keep circular buffers from ever trying to index negative numbers
            tick = WCommon.TICKS_PER_SECOND;
            
            ControlsManager.Activate();
            ChatUiManager.SendChatMessage += SendHostChatMessage;

            watch = new();
            isActivated = true;
        }

        void Update() {
            if(!isActivated) {
                Debug.Log("Not activated!");
                return;
            }
            
            percentageThroughTickCurrentFrame = watch.GetPercentageThroughTick();
            while(percentageThroughTickCurrentFrame > 1) {
                watch.AdvanceTick();
                StartNextTick();
                percentageThroughTickCurrentFrame = watch.GetPercentageThroughTick();
            }
        }

        protected override void OnDestroy() {
            ChatUiManager.SendChatMessage -= SendHostChatMessage;

            base.OnDestroy();
        }

        private void CreateHostPlayer() {
            WSEntity playerEntity = WSEntityManager.SpawnEntity(NetPrefabType.Player, true);
            playerEntity.positionsBuffer[tick] = new Vector3(0, 10, 0);
            AbstractPlayer player = playerEntity.GetComponent<AbstractPlayer>();
            ControlsManager.player = player;
            ControlsManager.player.EnablePlayer();

            HostPlayer = new(null, playerEntity);
        }


        private readonly WInputsSerializable noInputs = new();
        public void StartNextTick() {
            tick += 1;

            // Run server player inputs
            ControlsManager.PollAndControl(tick);

            // Run inputs of each client
            foreach (var peer in WNetManager.ConnectedPeers) {
                if(!WSPlayer.FromPeer(peer, out WSPlayer netPlayer))
                    continue;
                
                WInputsSerializable inputs = WSPlayerInputsSlotterManager.Instance.GetInputsOfAPlayer(tick, peer.Id) ?? noInputs;
                netPlayer.Entity.GetComponent<AbstractPlayer>().Control(inputs, tick);
            }
    
            WSEntityManager.PollFinalizeAdvanceEntities();

            // Only run further code if it's time for a snapshot
            if (tick % WCommon.TICKS_PER_SNAPSHOT == 0)
                RunSnapshot();
        }
        private void RunSnapshot() {
            foreach (var peer in WNetManager.ConnectedPeers) {
                SendUpdatesToPlayer(peer);
            }

            WSChunkManager.UnloadChunksMarkedForUnloading();
            WSChunkManager.ResetChunkUpdatesAndSnapshots();
        }
        private void SendUpdatesToPlayer(NetPeer peer) {
            WSPlayer player = WSPlayer.FromPeer(peer);
            if (player == null)
                return;

            // Get the initial snapshot packet from the chunk the player is in
            if(player.Entity != null) {
                WSChunk chunk = player.Entity.CurrentChunk;
                NetDataWriter unreliableChunkWriter = chunk.GetPrepared3x3UnreliableDeltaSnapshotPacket();

                // Save the position of the writer for this chunk for later use
                int chunkWriterPositionBeforeModification = unreliableChunkWriter.Length;
                
                // If the player changed chunks, append a packet for changed entities
                if(player.previousChunk != null) {
                    WSEntitiesLoadedDeltaPkt entitiesLoadedDeltaPkt = WSChunkManager.GetEntitiesLoadedDeltaPkt(
                        player.previousChunk.Coords,
                        player.Entity.ChunkPosition);

                    entitiesLoadedDeltaPkt?.Serialize(unreliableChunkWriter);
                }
                player.previousChunk = chunk;
                
                // Write player controller state
                INetSerializable genericControllerState = null;
                if(player.Entity.TryGetComponent(out DefaultController defaultController)) {
                    var defaultControllerState = defaultController.GetSerializableState(tick);
                    genericControllerState = defaultControllerState;
                } // else if spectator, etc.

                genericControllerState?.Serialize(unreliableChunkWriter);
                    
                // Send full snapshot to the player
                peer.Send(unreliableChunkWriter, DeliveryMethod.Unreliable);

                // Reset the writer from the chunk to before it was personalized
                unreliableChunkWriter.SetPosition(chunkWriterPositionBeforeModification);
            }

            // Also send reliable updates
            var reliableChunkWriter = player.Entity.CurrentChunk.GetPrepared3x3ReliableUpdatesPacket();
            // If no sources for reliable updates, don't send anything
            if(!player.ReliablePackets.HasPackets && reliableChunkWriter == null)
                return;

            // If 3x3 chunk had no reliable updates, create own multipacket (and don't bother resetting it)
            int? reliableChunkWriterPositionBeforeModification = reliableChunkWriter?.Length;
            if(reliableChunkWriter == null) {
                reliableChunkWriter = writer;
                WPacketCommunication.StartMultiPacket(writer, GetTick());
            }

            player.ReliablePackets.SerializeAndReset(writer);

            peer.Send(reliableChunkWriter, DeliveryMethod.ReliableOrdered);

            // Reset writer to before modification
            if(reliableChunkWriterPositionBeforeModification != null)
                reliableChunkWriter.SetPosition(reliableChunkWriterPositionBeforeModification.Value);
        }


        private bool ProcessPacketFromReader(NetPeer peer, NetDataReader reader, int tick, WPacketIdentifier packetType) {
            // Not the best architecture, but not that awful either
            switch (packetType) {
                case WPacketIdentifier.CJoinRequest: {
                    WCJoinRequestPkt joinRequest = new();
                    joinRequest.Deserialize(reader);
                    if (!joinRequest.s_isValid)
                        return false;

                    OnJoinReceived(joinRequest, peer);
                    return true;
                }
                case WPacketIdentifier.CGroupedInputs: {
                    if(peer.Tag == null)
                        return false;

                    WCGroupedInputsPkt groupedInputs = new();
                    groupedInputs.Deserialize(reader);
                    WSPlayerInputsSlotterManager.Instance.SetGroupedInputsOfPlayer(tick, peer.Id, groupedInputs);
                    return true;
                }
                case WPacketIdentifier.CChatMessage: {
                    if(peer.Tag == null)
                        return false;

                    Debug.Log("Got a chat message!");
                    WCChatMessagePkt chatMessage = new();
                    chatMessage.Deserialize(reader);
                    WSChatHandler.HandleChatMessage(chatMessage.message, peer, false);
                    return true;
                }
                default: {
                    print($"Could not handle packet of type {packetType}!");
                    return false;
                }
            }
        }


        private void OnJoinReceived(WCJoinRequestPkt joinRequest, NetPeer peer) {
            print($"Join packet received for {joinRequest.userName}");

            if(!joinRequest.s_isValid || !TryAddPlayer(peer, joinRequest.userName)) {
                Debug.Log("Invalid join packet!");
                peer.Disconnect();
            }
        }
        private bool TryAddPlayer(NetPeer peer, string userName) {
            if(WSPlayer.FromPeer(peer) != null)
                return false;

            WSEntity playerEntity = WSEntityManager.SpawnEntity(NetPrefabType.Player, true);
            WSPlayer wsPlayer = new(peer, playerEntity);
            peer.Tag = wsPlayer;
            PlayerJoined?.Invoke(wsPlayer);

            WPacketCommunication.StartMultiPacket(writer, tick);
            
            WSJoinAcceptPkt joinAcceptPacket = new() {
                userName = userName,
                tick = tick
            };
            joinAcceptPacket.Serialize(writer);

            WSChunk chunk = playerEntity.CurrentChunk;
            WSFullEntitiesSnapshotPkt fullEntitiesSnapshotPacket = new()
            {
                entities = new WEntitySerializable[chunk.PresentEntities.Count],
                isFullReset = true
            };
            int i=0;
            foreach(WSEntity entity in chunk.PresentEntities) {
                fullEntitiesSnapshotPacket.entities[i++] = entity.GetSerializedEntity(tick);
            }
            fullEntitiesSnapshotPacket.Serialize(writer);

            peer.Send(writer, DeliveryMethod.ReliableOrdered);

            return true;
        }
        private bool TryRemovePlayer(NetPeer peer) {
            WSPlayer player = WSPlayer.FromPeer(peer);
            if(player == null)
                return false;

            player.Entity.Kill(WEntityKillReason.Unload);
            //WSEntityManager.KillEntity(player.Entity.Id);
            
            WSPlayerInputsSlotterManager.Instance.RemovePlayer(peer.Id);

            return true;
        }


        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod) =>
            WPacketCommunication.ReadMultiPacket(peer, reader, ProcessPacketFromReader);

        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType) { }

        public void OnPeerConnected(NetPeer peer) => print($"Player connected: {peer.Address}!");

        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo) {
            print($"Player disconnected: {disconnectInfo.Reason}!");

            TryRemovePlayer(peer);
        }

        public void OnConnectionRequest(ConnectionRequest request) => request.AcceptIfKey(WCommon.CONNECTION_KEY);

        public void OnNetworkError(IPEndPoint endPoint, SocketError socketError) => print($"Network error: {socketError}");

        public void OnNetworkLatencyUpdate(NetPeer peer, int latency) {}

        private void SendHostChatMessage(string message) => WSChatHandler.HandleChatMessage(message, null, false);
    }
}
