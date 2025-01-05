using LiteNetLib;
using UnityEngine;
using LiteNetLib.Utils;
using System.Net;
using System.Net.Sockets;

using Networking.Shared;
using Controllers.Shared;

namespace Networking.Server {
    public class WSNetServer : MonoBehaviour, INetEventListener, ITicker {
        public static NetManager ServerNetManager { get; private set; }

        private static NetDataWriter writer = new();

        private static int tick;
        public static int Tick => tick;
        private static WWatch watch;
        public static WSPlayer HostPlayer { get; private set; }

        public int GetTick() {
            return tick;
        }
        public float GetPercentageThroughTick() {
            return watch.GetPercentageThroughTick();
        }

        public static WSNetServer Instance { get; private set; }
        public void Init(ushort port) {
            ServerNetManager.Start(port);
            print($"Running server on port {port}!");

            // Start one second ahead to keep circular buffers from ever trying to index negative numbers
            tick = WCommon.TICKS_PER_SECOND;

            // Initialize player
            WSEntity playerEntity = WSEntityManager.SpawnEntity(WPrefabId.Player, tick, true);
            playerEntity.positionsBuffer[tick] = new Vector3(0, 10, 0);
            IPlayer player = playerEntity.GetComponent<IPlayer>();
            ControlsManager.player = player;
            ControlsManager.player.EnablePlayer();

            HostPlayer = new();
            HostPlayer.Init(null, playerEntity);


            // Start listening for server-specific events
            ChatUiManager.SendChatMessage += (string message) => WSChatHandler.HandleChatMessage(message, null, false);

            watch = new();
            watch.Start();
        }

        private void CreatePlayer() {
            
        }


        private void Update() {
            ServerNetManager.PollEvents();

            while(GetPercentageThroughTick() > 1) {
                watch.AdvanceTick();
                StartNextTick();
            }
        }


        private readonly WInputsSerializable noInputs = new();
        public void StartNextTick() {
            tick += 1;

            // Run server player inputs
            ControlsManager.PollAndControl(tick);

            // Run inputs of each client
            foreach (var peer in ServerNetManager.ConnectedPeerList) {
                if(!WSPlayer.FromPeer(peer, out WSPlayer netPlayer))
                    continue;
                
                WInputsSerializable inputs = WsPlayerInputsSlotter.GetInputsOfAPlayer(tick, peer.Id) ?? noInputs;
                netPlayer.Entity.GetComponent<IPlayer>().Control(inputs, tick);
            }

            WSEntityManager.PollFinalizeAdvanceEntities();

            // Only run further code if it's time for a snapshot
            if (tick % WCommon.TICKS_PER_SNAPSHOT == 0)
                RunSnapshot();
        }
        private void RunSnapshot() {
            foreach (var peer in ServerNetManager.ConnectedPeerList) {
                SendUpdatesToPlayer(peer);
            }

            WSChunkManager.UnloadChunksMarkedForUnloading();
            WSChunkManager.ResetChunkUpdatesAndSnapshots();
        }
        private void SendUpdatesToPlayer(NetPeer peer) {
            WSPlayer netPlayer = WSPlayer.FromPeer(peer);
            if (netPlayer == null)
                return;

            // Get the initial snapshot packet from the chunk the player is in
            WSChunk chunk = netPlayer.Entity.CurrentChunk;
            writer = chunk.GetPrepared3x3UnreliableDeltaSnapshotPacket();

            // Save the position of the writer for this chunk for later use
            int writerPositionBeforeModification = writer.Length;
            
            // If the player changed chunks, append a packet for changed entities
            if(netPlayer.previousChunk != null) {
                WSEntitiesLoadedDeltaPkt entitiesLoadedDeltaPkt = WSChunkManager.GetEntitiesLoadedDeltaPkt(
                    netPlayer.previousChunk.Coords,
                    netPlayer.Entity.ChunkPosition);

                if(entitiesLoadedDeltaPkt != null)
                    entitiesLoadedDeltaPkt.Serialize(writer);
            }
            netPlayer.previousChunk = chunk;
            
            // Write player controller state
            INetSerializable genericControllerState = null;
            if(netPlayer.Entity.TryGetComponent(out DefaultController defaultController)) {
                var defaultControllerState = defaultController.GetSerializableState(tick);
                genericControllerState = defaultControllerState;
            } // else if spectator, etc.

            if(genericControllerState != null) {
                genericControllerState.Serialize(writer);
            }
                
            // Send full snapshot to the player, then reset the writer from the chunk to before it was personalized
            peer.Send(writer, DeliveryMethod.Unreliable);
            writer.SetPosition(writerPositionBeforeModification);

            // Also send reliable updates
            var reliableUpdates = chunk.GetPrepared3x3ReliableUpdatesPacket();
            if(reliableUpdates != null) {
                peer.Send(reliableUpdates, DeliveryMethod.ReliableOrdered);
            }
            
        }


        private bool ProcessPacketFromReader(
            NetPeer peer,
            NetDataReader reader,
            int tick,
            WPacketType packetType) {

            switch (packetType) {

                case WPacketType.CJoinRequest: {
                    WCJoinRequestPkt joinRequest = new();
                    joinRequest.Deserialize(reader);
                    if (!joinRequest.s_isValid)
                        return false;

                    OnJoinReceived(joinRequest, peer);
                    break;
                }
                case WPacketType.CGroupedInputs: {
                    if(peer.Tag == null)
                        return false;

                    WCGroupedInputsPkt groupedInputs = new();
                    groupedInputs.Deserialize(reader);
                    WsPlayerInputsSlotter.SetGroupedInputsOfPlayer(tick, peer.Id, groupedInputs);
                    break;
                }
                case WPacketType.CChatMessage: {
                    if(peer.Tag == null)
                        return false;

                    Debug.Log("Got a chat message!");
                    WCChatMessagePkt chatMessage = new();
                    chatMessage.Deserialize(reader);
                    WSChatHandler.HandleChatMessage(chatMessage.message, peer, false);
                    break;
                }

                default: {
                    print($"Could not handle packet of type {packetType}!");
                    return false;
                }
            }

            return true;
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

            WsPlayerInputsSlotter.AddPlayer(peer.Id);

            WSEntity playerEntity = WSEntityManager.SpawnEntity(WPrefabId.Player, Tick, true);

            WSPlayer netPlayer = new();
            netPlayer.Init(peer, playerEntity);
            peer.Tag = netPlayer;

            WSJoinAcceptPkt joinAcceptPacket = new() {
                userName = userName,
                playerEntityId = playerEntity.Id,
                tick = Tick
            };

            WSEntitiesLoadedDeltaPkt entitiesLoadPacket = WSChunkManager.GetEntitiesLoadedDeltaPkt(null, Vector2Int.zero);
            
            WPacketCommunication.StartMultiPacket(writer, tick);
            joinAcceptPacket.Serialize(writer);
            entitiesLoadPacket.Serialize(writer);
            peer.Send(writer, DeliveryMethod.ReliableUnordered);

            return true;
        }
        private bool TryRemovePlayer(NetPeer peer) {
            WSPlayer player = WSPlayer.FromPeer(peer);
            if(player == null)
                return false;

            WSEntityManager.KillEntity(player.Entity.Id);
            
            WsPlayerInputsSlotter.RemovePlayer(peer.Id);

            return true;
        }


        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod) {
            WPacketCommunication.ReadMultiPacket(peer, reader, ProcessPacketFromReader);
        }


        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType) { }


        public void OnPeerConnected(NetPeer peer) {
            print($"Player connected: {peer.Address}!");
        }


        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo) {
            print($"Player disconnected: {disconnectInfo.Reason}!");

            TryRemovePlayer(peer);
        }


        public void OnConnectionRequest(ConnectionRequest request) {
            Debug.Log("Received a connection request!");
            request.AcceptIfKey("WW 0.01");
        }


        public void OnNetworkError(IPEndPoint endPoint, SocketError socketError) {
            print($"Network error: {socketError}");
        }


        public void OnNetworkLatencyUpdate(NetPeer peer, int latency) {

        }

        private void OnDestroy() {
            ServerNetManager.Stop();
        }

        
        void Awake() {
            if(Instance != null)
                Destroy(gameObject);

            Instance = this;
            DontDestroyOnLoad(gameObject);

            ServerNetManager = new NetManager(this) {
                AutoRecycle = true,
                IPv6Enabled = false
            };
        }
    }
}
