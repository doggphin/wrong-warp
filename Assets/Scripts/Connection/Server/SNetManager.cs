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
    [RequireComponent(typeof(SEntityManager))]
    [RequireComponent(typeof(SPlayerInputsSlotterManager))]
    [RequireComponent(typeof(SInventoryManager))]
    [RequireComponent(typeof(SPacketUnpacker))]
    [RequireComponent(typeof(SChatHandler))]
    [RequireComponent(typeof(SChunkManager))]
    public class SNetManager : BaseSingleton<SNetManager>, ITicker, INetEventListener {
        private SEntityManager entityManager;
        private static NetDataWriter writer = new();

        private static int tick;
        public int GetTick() => tick;
        public static int Tick => Instance.GetTick();
        private static WWatch watch;
        private float percentageThroughTickCurrentFrame;
        public float GetPercentageThroughTickCurrentFrame() => percentageThroughTickCurrentFrame;
        public float GetPercentageThroughTick() => watch.GetPercentageThroughTick();
        public static SPlayer HostPlayer { get; private set; }

        public static Action<SPlayer> PlayerJoined;
        public static Action<SPlayer> PlayerLeft;

        private bool isActivated;
        public void Activate() {
            if(isActivated)
                return;

            entityManager = GetComponent<SEntityManager>();
            CreateHostPlayer();
            // Start one second ahead to keep circular buffers from ever trying to index negative numbers
            tick = NetCommon.TICKS_PER_SECOND;
            
            ControlsManager.Activate();
            ChatUiManager.SendChatMessage += SendHostChatMessage;
            CPacket<CJoinRequestPkt>.ApplyUnticked += HandleJoinRequest;

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
            CPacket<CJoinRequestPkt>.ApplyUnticked -= HandleJoinRequest;
            ControlsManager.Deactivate();

            base.OnDestroy();
        }

        private void CreateHostPlayer() {
            SEntity playerEntity = SEntityManager.SpawnEntity(EntityPrefabId.Player, true);
            playerEntity.positionsBuffer[tick] = new Vector3(0, 10, 0);
            AbstractPlayer player = playerEntity.GetComponent<AbstractPlayer>();
            ControlsManager.SetPlayer(player);
            player.EnablePlayer();

            HostPlayer = new(null, playerEntity);
        }


        private readonly InputsSerializable noInputs = new();
        public void StartNextTick() {
            tick += 1;

            // Run server player inputs
            ControlsManager.PollAndControl(tick);

            // Run inputs of each client
            foreach(NetPeer peer in WWNetManager.ConnectedPeers) {
                if(!peer.TryGetWSPlayer(out SPlayer player))
                    continue;

                if(!SPlayerInputsSlotterManager.TryGetInputsOfAPlayer(tick, player, out InputsSerializable inputs))
                    inputs = noInputs;
                
                player.Entity.GetComponent<AbstractPlayer>().Control(inputs, tick);
            }
    
            SEntityManager.PollFinalizeAdvanceEntities();

            // Only run further code if it's time for a snapshot
            if (tick % NetCommon.TICKS_PER_SNAPSHOT == 0)
                RunSnapshot();
        }
        private void RunSnapshot() {
            foreach (var peer in WWNetManager.ConnectedPeers) {
                SendUpdatesToPlayer(peer);
            }

            SChunkManager.UnloadChunksMarkedForUnloading();
            SChunkManager.ResetChunkUpdatesAndSnapshots();
        }
        private void SendUpdatesToPlayer(NetPeer peer) {
            if(!peer.TryGetWSPlayer(out var player))
                return;

            // Get the initial snapshot packet from the chunk the player is in
            if(player.Entity != null) {
                SChunk chunk = player.Entity.CurrentChunk;
                NetDataWriter unreliableChunkWriter = chunk.GetPrepared3x3UnreliableDeltaSnapshotPacket();

                // Save the position of the writer for this chunk for later use
                int chunkWriterPositionBeforeModification = unreliableChunkWriter.Length;
                
                // If the player changed chunks, append a packet for changed entities
                if(player.previousChunk != null) {
                    WSEntitiesLoadedDeltaPkt entitiesLoadedDeltaPkt = SChunkManager.GetEntitiesLoadedDeltaPkt(
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
                PacketCommunication.StartMultiPacket(writer, GetTick());
            }

            player.ReliablePackets.SerializeAndReset(writer);

            peer.Send(reliableChunkWriter, DeliveryMethod.ReliableOrdered);

            // Reset writer to before modification
            if(reliableChunkWriterPositionBeforeModification != null)
                reliableChunkWriter.SetPosition(reliableChunkWriterPositionBeforeModification.Value);
        }

        private bool TryAddPlayer(NetPeer peer, string userName) {
            if(peer.TryGetWSPlayer(out _))
                return false;

            SEntity playerEntity = SEntityManager.SpawnEntity(EntityPrefabId.Player, true);
            SPlayer wsPlayer = new(peer, playerEntity);
            peer.Tag = wsPlayer;
            PlayerJoined?.Invoke(wsPlayer);

            PacketCommunication.StartMultiPacket(writer, tick);
            
            SJoinAcceptPkt joinAcceptPacket = new() {
                userName = userName,
                tick = tick
            };
            joinAcceptPacket.Serialize(writer);

            SChunk chunk = playerEntity.CurrentChunk;
            WSFullEntitiesSnapshotPkt fullEntitiesSnapshotPacket = new()
            {
                entities = new WEntitySerializable[chunk.PresentEntities.Count],
                isFullReset = true
            };
            int i=0;
            foreach(SEntity entity in chunk.PresentEntities) {
                fullEntitiesSnapshotPacket.entities[i++] = entity.GetSerializedEntity(tick);
            }
            fullEntitiesSnapshotPacket.Serialize(writer);

            peer.Send(writer, DeliveryMethod.ReliableOrdered);

            return true;
        }
        private bool TryRemovePlayer(NetPeer peer) {
            if(!peer.TryGetWSPlayer(out var player))
                return false;

            player.Entity.Kill(WEntityKillReason.Unload);
            //WSEntityManager.KillEntity(player.Entity.Id);
            
            SPlayerInputsSlotterManager.RemovePlayer(player);

            return true;
        }


        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod) {
            int tick = reader.GetInt();
            while(reader.AvailableBytes >= sizeof(ushort)) {
                BasePacket packet = SPacketUnpacker.DeserializeNextPacket(reader);
                packet.Sender = peer;
                packet.BroadcastApply(tick);
            }
        }

        private void HandleJoinRequest(CJoinRequestPkt joinRequest, NetPeer peer) {
            print($"Join packet received for {joinRequest.userName}");

            if(!TryAddPlayer(peer, joinRequest.userName)) {
                Debug.Log("Invalid join packet!");
                peer.Disconnect();
            }
        }
        //WSPacketUnpacker.ConsumeAllPackets(tick, reader);
        //WPacketCommunication.ReadMultiPacket(peer, reader, ProcessPacketFromReader);
        

        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType) { }

        public void OnPeerConnected(NetPeer peer) => print($"Player connected: {peer.Address}!");

        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo) {
            print($"Player disconnected: {disconnectInfo.Reason}!");

            TryRemovePlayer(peer);
        }

        public void OnConnectionRequest(ConnectionRequest request) => request.AcceptIfKey(NetCommon.CONNECTION_KEY);

        public void OnNetworkError(IPEndPoint endPoint, SocketError socketError) => print($"Network error: {socketError}");

        public void OnNetworkLatencyUpdate(NetPeer peer, int latency) {}

        private void SendHostChatMessage(string message) => SChatHandler.HandleChatMessage(message, null, false);
    }
}
