using LiteNetLib;
using UnityEngine;
using LiteNetLib.Utils;
using System.Net;
using System.Net.Sockets;

using Networking.Shared;
using Controllers.Shared;
using System;

namespace Networking.Server {
    [RequireComponent(typeof(SEntityManager))]
    [RequireComponent(typeof(SPlayerInputsSlotterManager))]
    [RequireComponent(typeof(SInventoryManager))]
    [RequireComponent(typeof(SPacketUnpacker))]
    [RequireComponent(typeof(SPacketPacker))]
    [RequireComponent(typeof(SChatHandler))]
    [RequireComponent(typeof(SChunkManager))]
    [RequireComponent(typeof(ControlsManager))]
    public class SNetManager : BaseSingleton<SNetManager>, ITicker, INetEventListener {
        private ControlsManager controlsManager;
        private SPacketPacker packetPacker;

        private static int tick;
        public int GetTick() => tick;
        public static int Tick => Instance.GetTick();

        private static NetDataWriter baseWriter = new();
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

            CreateHostPlayer();
            // Start one second ahead to keep circular buffers from ever trying to index negative numbers
            tick = NetCommon.TICKS_PER_SECOND;
            
            controlsManager = GetComponent<ControlsManager>();
            packetPacker = GetComponent<SPacketPacker>();
            ControlsManager.ActivateControls();
            ChatUiManager.SendChatMessage += SendHostChatMessage;
            CPacket<CJoinRequestPkt>.ApplyUnticked += HandleJoinRequest;

            watch = new();
            isActivated = true;
        }

        void Update() {
            if(!isActivated) {
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
            ControlsManager.DeactivateControls();

            base.OnDestroy();
        }


        private void CreateHostPlayer() {
            HostPlayer = new(null);
            SEntity playerEntity = SEntityManager.Instance.SpawnEntity(EntityPrefabId.Player, null, null, null, HostPlayer);
            
            playerEntity.positionsBuffer[tick] = new Vector3(0, 10, 0);
            AbstractPlayer player = playerEntity.GetComponent<AbstractPlayer>();
            ControlsManager.SetPlayer(player);
            player.EnablePlayer();
        }


        private readonly InputsSerializable noInputs = new();
        public void StartNextTick() {
            tick += 1;

            // Run server player inputs
            controlsManager.PollAndControl(tick);

            // Run inputs of each client
            foreach(NetPeer peer in WWNetManager.ConnectedPeers) {
                if(!peer.TryGetWSPlayer(out SPlayer player))
                    continue;

                if(!SPlayerInputsSlotterManager.TryGetInputsOfAPlayer(tick, player, out InputsSerializable inputs))
                    inputs = noInputs;
                
                player.Entity.GetComponent<AbstractPlayer>().Control(inputs, tick);
            }
    
            SEntityManager.Instance.AdvanceEntities();

            // Only run further code if it's time for a snapshot
            if (tick % NetCommon.TICKS_PER_SNAPSHOT == 0)
                RunSnapshot();
        }

        private void RunSnapshot() {
            SEntityManager.Instance.UpdateChunksOfEntities();

            foreach (var peer in WWNetManager.ConnectedPeers) {
                SendUpdatesToPlayer(peer);
            }

            SChunkManager.Instance.CleanupAfterSnapshot();
        }
        

        private void SendUpdatesToPlayer(NetPeer peer) {
            NetDataWriter testWriter = new();

            if(!peer.TryGetWSPlayer(out var player))
                return;

            bool hasReliableUpdates = false;
            packetPacker.StartPacketCollection(testWriter, tick);
            if(player.Entity != null) {
                if(SChunkManager.Instance.TryGetReliablePlayerUpdates(player, out NetDataWriter reliableChunkUpdates)) {
                    testWriter.Append(reliableChunkUpdates);
                    hasReliableUpdates = true;
                }
                if((player.ReliablePackets?.SerializeAndReset(testWriter, false)).GetValueOrDefault(false)) {
                    hasReliableUpdates = true;
                }
            }
            if(hasReliableUpdates) {
                peer.Send(testWriter, DeliveryMethod.ReliableOrdered);
            }
            
            testWriter.Reset();
            SPacketFragmenter.PutFragmentedPacketHeader(testWriter, tick);
            bool hasUnreliableUpdates = false;
            if(SChunkManager.Instance.TryGetUnreliablePlayerUpdates(player, out NetDataWriter unreliableChunkUpdates)) {
                testWriter.Append(unreliableChunkUpdates);
                hasUnreliableUpdates = true;
            }
            // TODO: abstract this to work with any controller
            if(player.Entity.TryGetComponent(out DefaultController defaultController)) {
                var defaultControllerState = defaultController.GetSerializableState(tick);
                defaultControllerState.Serialize(testWriter);
                hasUnreliableUpdates = true;
            }
            if(hasUnreliableUpdates) {
                var fragmentedWriters = SPacketFragmenter.FragmentPacketCollection(testWriter, tick);
                foreach(var fragmentedWriter in fragmentedWriters) {
                    peer.Send(fragmentedWriter, DeliveryMethod.Unreliable);
                }
            }
        }


        private bool TryAddPlayer(NetPeer peer, string userName) {
            if(peer.TryGetWSPlayer(out _))
                return false;

            SPlayer player = new(peer);
            SEntity playerEntity = SEntityManager.Instance.SpawnEntity(EntityPrefabId.Player, null, null, null, player);

            peer.Tag = player;
            PlayerJoined?.Invoke(player);

            packetPacker.StartPacketCollection(baseWriter, tick);
            
            SJoinAcceptPkt joinAcceptPacket = new() {
                userName = userName,
                tick = tick
            };
            joinAcceptPacket.Serialize(baseWriter);

            SFullEntitiesSnapshotPkt fullEntitiesSnapshot = playerEntity.Chunk.GetFullEntitiesSnapshot(tick);
            fullEntitiesSnapshot.Serialize(baseWriter);

            peer.Send(baseWriter, DeliveryMethod.ReliableOrdered);
            return true;
        }

        private bool TryRemovePlayer(NetPeer peer) {
            if(!peer.TryGetWSPlayer(out var player))
                return false;

            player.Entity.StartDeath(WEntityKillReason.Unload);
            //WSEntityManager.KillEntity(player.Entity.Id);
            
            SPlayerInputsSlotterManager.RemovePlayer(player);

            return true;
        }

        private void HandleJoinRequest(CJoinRequestPkt joinRequest, NetPeer peer) {
            print($"Join packet received for {joinRequest.userName}");

            if(!TryAddPlayer(peer, joinRequest.userName)) {
                Debug.Log("Invalid join packet!");
                peer.Disconnect();
            }
        }

        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod) {
            int tick = reader.GetInt();
            while(reader.AvailableBytes >= sizeof(ushort)) {
                BasePacket packet = SPacketUnpacker.DeserializeNextPacket(reader);
                packet.Sender = peer;
                packet.BroadcastApply(tick);
            }
        }

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
