using LiteNetLib.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;

using Networking.Shared;
using Unity.VisualScripting;
using System.Runtime.InteropServices.WindowsRuntime;

namespace Networking.Server {
    public class WSChunk {
        private HashSet<WSEntity> presentLoaders = new();
        public HashSet<WSEntity> PresentEntities { get; private set; } = new();
        public List<INetSerializable>[] ReliableUpdates { get; private set; } = new List<INetSerializable>[WCommon.TICKS_PER_SNAPSHOT];
        private List<INetSerializable>[] unreliableGeneralUpdates = new List<INetSerializable>[WCommon.TICKS_PER_SNAPSHOT];
        private Dictionary<int, List<INetSerializable>>[] unreliableEntityUpdates = new Dictionary<int, List<INetSerializable>>[WCommon.TICKS_PER_SNAPSHOT];
        private bool isLoaded = false;
        public Vector2Int Coords { get; private set; }

        public void Load(Vector2Int coords) {
            isLoaded = true;

            for (int i = 0; i < WCommon.TICKS_PER_SNAPSHOT; i++) {
                unreliableGeneralUpdates[i] = new();
                unreliableEntityUpdates[i] = new();
                ReliableUpdates[i] = new();
            }

            Coords = coords;
        }


        // Chunks will write their own WSChunkDeltaSnapshotPkts and cache them for each group of ticks.
        // Each player within each chunk is sent the WSChunkDeltaSnapshotPkt of each chunk in a 3x3 radius around them.
        // Work can be reduced by reusing individual chunk WSChunkDeltaSnapshotPkts and sending the same combined 3x3 chunks to players in same chunk
        private WSChunkDeltaSnapshotPkt unreliableDeltaSnapshot = null;
        private bool isUnreliableDeltaSnapshot3x3PktWritten = false;
        private NetDataWriter unreliableDeltaSnapshot3x3PktWriter = new();
        public NetDataWriter GetPrepared3x3UnreliableDeltaSnapshotPacket() {
            if (isUnreliableDeltaSnapshot3x3PktWritten) {
                Debug.Log("Returning already written snapshot!");
                return unreliableDeltaSnapshot3x3PktWriter;
            }

            unreliableDeltaSnapshot3x3PktWriter.Reset();

            WPacketCommunication.StartMultiPacket(unreliableDeltaSnapshot3x3PktWriter, WSNetServer.Tick);
            GetUnreliableDeltaSnapshot().Serialize(unreliableDeltaSnapshot3x3PktWriter);

            WSChunk[] neighbors = WSChunkManager.GetNeighboringChunks(Coords, false, false);
            for (int i = 0; i < 8; i++) {
                if (neighbors[i] == null) {
                    Debug.Log("A surrounding chunk was null!");
                    continue;
                }
                neighbors[i].GetUnreliableDeltaSnapshot().Serialize(unreliableDeltaSnapshot3x3PktWriter);
            }

            isUnreliableDeltaSnapshot3x3PktWritten = true;

            return unreliableDeltaSnapshot3x3PktWriter;
        }
        public WSChunkDeltaSnapshotPkt GetUnreliableDeltaSnapshot() {
            if (unreliableDeltaSnapshot != null)
                return unreliableDeltaSnapshot;

            unreliableDeltaSnapshot = new() { s_generalUpdates = unreliableGeneralUpdates, s_entityUpdates = unreliableEntityUpdates };

            return unreliableDeltaSnapshot;
        }

        private NetDataWriter reliableUpdates3x3PktWriter = new();
        private bool isReliableUpdates3x3Written = false;
        public NetDataWriter GetPrepared3x3ReliableUpdatesPacket() {
            // If 3x3 reliable updates have already been written + cached, use that
            if(isReliableUpdates3x3Written)
                return reliableUpdates3x3PktWriter;

            reliableUpdates3x3PktWriter.Reset();

            // Serialize all ReliableUpdates in each surrounding chunk, including ourselves
            foreach(WSChunk chunk in WSChunkManager.GetNeighboringChunks(Coords, true, false)) {
                foreach(INetSerializable packet in chunk.ReliableUpdates) {
                    packet.Serialize(reliableUpdates3x3PktWriter);
                }
            }

            isReliableUpdates3x3Written = true;
            return reliableUpdates3x3PktWriter;
        }


        public void ResetUpdates() {
            for (int i = 0; i < WCommon.TICKS_PER_SNAPSHOT; i++) {
                unreliableGeneralUpdates[i].Clear();
                unreliableEntityUpdates[i].Clear();
                ReliableUpdates[i].Clear();
            }

            unreliableDeltaSnapshot = null;
            isUnreliableDeltaSnapshot3x3PktWritten = false;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="tick"></param>
        /// <param name="id"></param>
        /// <param name="update"></param>
        /// <returns> Whether this chunk is loaded. </returns>
        public bool AddEntityUpdate(int tick, int id, INetSerializable update) {
            if (!isLoaded)
                return false;

            // Want insertAt to start at 0, not 1. Need to do add ticksPerSnapshot - 1 to make it start at 0
            int insertAt = (tick + WCommon.TICKS_PER_SNAPSHOT - 1) % WCommon.TICKS_PER_SNAPSHOT;

            if (!unreliableEntityUpdates[insertAt].TryGetValue(id, out var entityUpdatesList)) {
                List<INetSerializable> newEntityUpdatesList = new();

                unreliableEntityUpdates[insertAt][id] = newEntityUpdatesList;
                entityUpdatesList = newEntityUpdatesList;
            }

            entityUpdatesList.Add(update);

            return true;
        }


        /// <returns> Whether the chunk is loaded. </returns>
        public bool AddGenericUpdate(int tick, INetSerializable update, bool reliable) {
            if (!isLoaded)
                return false;
            
            List<INetSerializable>[] updatesToAppendTo = reliable ? ReliableUpdates : unreliableGeneralUpdates;
            updatesToAppendTo[tick % WCommon.TICKS_PER_SNAPSHOT].Add(update);

            return true;
        }


        public void Unload() {
            isLoaded = false;
        }


        public void AddChunkLoader(WSEntity entity) {
            if (presentLoaders.Count == 0) {
                WSChunkManager.chunksMarkedToUnload.Remove(Coords);
            }
                

            presentLoaders.Add(entity);
        }


        public void RemoveChunkLoader(WSEntity loader) {
            presentLoaders.Remove(loader);

            if (presentLoaders.Count == 0) {
                WSChunkManager.chunksMarkedToUnload.Add(Coords);
            }  
        }
    }
}
