using LiteNetLib;
using UnityEngine;
using LiteNetLib.Utils;
using System.Net;
using System.Net.Sockets;

using Networking.Shared;
using Controllers.Shared;

namespace Networking.Server {
    public class WSNetServer : MonoBehaviour, INetEventListener {
        public static NetManager ServerNetManager { get; private set; }

        private static NetDataWriter writer = new();

        private static int tick;
        public static int Tick => tick;
        private static WWatch watch;
        public static float PercentageThroughTick => watch.GetPercentageThroughTick();

        public static WSNetServer Instance { get; private set; }
        public void Init() {
            ServerNetManager.Start(WCommon.WRONGWARP_PORT);
            print($"Running server on port {WCommon.WRONGWARP_PORT}!");

            // Start one second ahead to keep circular buffers from ever trying to index negative numbers
            tick = WCommon.TICKS_PER_SECOND;

            WSEntity playerEntity = WSEntityManager.SpawnEntity(WPrefabId.Player, tick, true);
            playerEntity.positionsBuffer[tick] = new Vector3(0, 10, 0);
            IPlayer player = playerEntity.GetComponent<IPlayer>();
            player.ServerInit();
            
            ControlsManager.player = player;
            ControlsManager.player.EnablePlayer();

            watch = new();
            watch.Start();
        }


        private void Update() {
            ServerNetManager.PollEvents();

            while(PercentageThroughTick > 1) {
                watch.AdvanceTick();
                StartNextTick();
            }
        }


        private readonly WInputsSerializable noInputs = new();
        public void StartNextTick() {
            tick += 1;
            // Run server player inputs
            ControlsManager.PollInputs(null, tick);
            // Run inputs of each client
            foreach (var peer in ServerNetManager.ConnectedPeerList) {
                WSPlayer netPlayer = WSPlayer.FromPeer(peer);
                if (netPlayer == null)
                    continue;
                
                WInputsSerializable inputs = WsPlayerInputsSlotter.GetInputsOfAPlayer(tick, peer.Id) ?? noInputs;

                netPlayer.Entity.GetComponent<IPlayer>().Control(inputs, tick);
            }
            // Finalize entities
            WSEntityManager.PollFinalizeAdvanceEntities();
            // Only run further code if it's time for a snapshot
            if (tick % WCommon.TICKS_PER_SNAPSHOT != 0)
                return;
            // Get and send snapshots of all chunks players are in to all players in those chunks
            foreach (var peer in ServerNetManager.ConnectedPeerList) {
                WSPlayer netPlayer = WSPlayer.FromPeer(peer);
                if (netPlayer == null)
                    continue;

                if(netPlayer.previousChunk != null) {
                    WSEntitiesLoadedDeltaPkt entitiesLoadedDeltaPkt = WSChunkManager.GetEntitiesLoadedDeltaPkt(
                        netPlayer.previousChunk.Coords,
                        netPlayer.Entity.ChunkPosition);

                    if(entitiesLoadedDeltaPkt != null) {
                        writer.Reset();
                        WPacketComms.StartMultiPacket(writer, tick);
                        WPacketComms.AddToMultiPacket(writer, entitiesLoadedDeltaPkt);

                        peer.Send(writer, DeliveryMethod.ReliableUnordered);
                    }
                }

                WSChunk chunk = netPlayer.Entity.CurrentChunk;
                NetDataWriter chunkWriter = chunk.GetPrepared3x3SnapshotPacket();
                peer.Send(chunkWriter, DeliveryMethod.Unreliable);

                netPlayer.previousChunk = chunk;
            }
            // Unload + reset chunks
            WSChunkManager.UnloadChunksMarkedForUnloading();
            WSChunkManager.ResetChunkUpdatesAndSnapshots();
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

                    return true;
                }

                case WPacketType.CGroupedInputs: {
                    if(peer.Tag == null)
                        return false;

                    WCGroupedInputsPkt groupedInputsPkt = new();
                    groupedInputsPkt.Deserialize(reader);

                    WsPlayerInputsSlotter.SetGroupedInputsOfPlayer(tick, peer.Id, groupedInputsPkt);
                    
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

            WsPlayerInputsSlotter.AddPlayer(peer.Id);

            WSEntity playerEntity = WSEntityManager.SpawnEntity(WPrefabId.Player, Tick, true);

            IPlayer playerPlayer = playerEntity.GetComponent<IPlayer>();
            playerPlayer.ServerInit();

            WSPlayer netPlayer = new();
            netPlayer.Init(peer, playerEntity);
            peer.Tag = netPlayer;

            WSJoinAcceptPkt joinAcceptPacket = new() {
                userName = userName,
                playerEntityId = playerEntity.Id,
                tick = Tick
            };

            WSEntitiesLoadedDeltaPkt entitiesLoadPacket = WSChunkManager.GetEntitiesLoadedDeltaPkt(null, Vector2Int.zero);
            
            WPacketComms.StartMultiPacket(writer, tick);
            WPacketComms.AddToMultiPacket(writer, joinAcceptPacket);
            WPacketComms.AddToMultiPacket(writer, entitiesLoadPacket);
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
            WPacketComms.ReadMultiPacket(peer, reader, ProcessPacketFromReader, true);
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
            UnityEngine.Debug.unityLogger.Log("Received a connection request!");
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

        
        private void Awake() {
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
