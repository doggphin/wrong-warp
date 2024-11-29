using LiteNetLib;
using UnityEngine;
using LiteNetLib.Utils;
using System.Net;
using System.Net.Sockets;

using Networking.Shared;
using Controllers.Shared;

namespace Networking.Server {
    public class WSNetServer : MonoBehaviour, INetEventListener {
        public NetManager ServerNetManager { get; private set; }

        private NetDataWriter writer = new();

        private int tick;
        public static int Tick => Instance.tick;

        public static WSNetServer Instance { get; private set; }
        

        public void Init() {
            ServerNetManager.Start(WCommon.WRONGWARP_PORT);

            tick = 0;
            Debug.Log($"Running server on port {WCommon.WRONGWARP_PORT}!");

            /*WSEntity entity = WSEntityManager.SpawnEntity(WPrefabId.Test, true);
            entity.gameObject.AddComponent<SpinnerTest>();*/

            WSEntity playerEntity = WSEntityManager.SpawnEntity(WPrefabId.Player, true);
            IPlayer player = playerEntity.GetComponent<IPlayer>();
            player.ServerInit();
            
            ControlsManager.player = player;
            ControlsManager.player.EnablePlayer();
        }


        private readonly WInputsSerializable noInputs = new();
        public void AdvanceTick() {
            WSEntityManager.AdvanceTick(tick);
            
            ControlsManager.Poll(null);

            // If this tick isn't an update tick, advance to the next one
            foreach (var peer in ServerNetManager.ConnectedPeerList) {
                WSPlayer netPlayer = WSPlayer.FromPeer(peer);
                if (netPlayer == null)
                    continue;
                
                WInputsSerializable inputs = WsPlayerInputsSlotter.GetInputsOfPlayer(tick, peer.Id) ?? noInputs;
                netPlayer.Entity.GetComponent<IPlayer>().Control(inputs);

                if (tick % WCommon.TICKS_PER_SNAPSHOT == 0) {
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
            }

            Debug.Log(tick);
            if (tick++ % WCommon.TICKS_PER_SNAPSHOT != 0)
                return;

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

                    WsPlayerInputsSlotter.SetGroupedInputsOfPlayer(Tick, peer.Id, groupedInputsPkt);
                    
                    return true;
                }

                default: {
                    Debug.Log($"Could not handle packet of type {packetType}!");
                    return false;
                }
            }
        }


        private void OnJoinReceived(WCJoinRequestPkt joinRequest, NetPeer peer) {
            Debug.Log($"Join packet received for {joinRequest.userName}");

            WsPlayerInputsSlotter.AddPlayer(peer.Id);

            WSEntity playerEntity = WSEntityManager.SpawnEntity(WPrefabId.Player, true);
            playerEntity.currentPosition = new Vector3(5, 5, 5);

            IPlayer playerPlayer = playerEntity.GetComponent<IPlayer>();
            playerPlayer.ServerInit();

            WSPlayer netPlayer = new WSPlayer();
            netPlayer.Init(peer, playerEntity);
            peer.Tag = netPlayer;

            WSJoinAcceptPkt joinAcceptPacket = new() {
                userName = joinRequest.userName,
                playerEntityId = playerEntity.Id,
                tick = Tick
            };

            WSEntitiesLoadedDeltaPkt entitiesLoadPacket = WSChunkManager.GetEntitiesLoadedDeltaPkt(null, Vector2Int.zero);
            
            WPacketComms.StartMultiPacket(writer, tick);
            WPacketComms.AddToMultiPacket(writer, joinAcceptPacket);
            WPacketComms.AddToMultiPacket(writer, entitiesLoadPacket);
            peer.Send(writer, DeliveryMethod.ReliableUnordered);
        }


        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod) {
            WPacketComms.ReadMultiPacket(peer, reader, ProcessPacketFromReader, true);
        }


        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType) { }


        public void OnPeerConnected(NetPeer peer) {
            Debug.Log($"Player connected: {peer.Address}!");
        }


        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo) {
            Debug.Log($"Player disconnected: {disconnectInfo.Reason}!");

            //WNetPlayer netPlayer = (WNetPlayer)peer.Tag;
            //WNetEntityManager.KillEntity(netPlayer.Entity.Id);
        }


        public void OnConnectionRequest(ConnectionRequest request) {
            Debug.unityLogger.Log("Received a connection request!");
            request.AcceptIfKey("WW 0.01");
        }


        public void OnNetworkError(IPEndPoint endPoint, SocketError socketError) {
            Debug.Log($"Network error: {socketError}");
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


        private void Update() {
            ServerNetManager.PollEvents();
        }
    }
}
