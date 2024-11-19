using LiteNetLib;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.CompilerServices;

namespace Networking.Shared {
    public class WNetChunkManager : MonoBehaviour {
        private const float chunkSize = 32;
        private Dictionary<Vector2Int, WNetChunk> loadedChunks;
        public static WNetChunkManager Instance { get; private set; }

        public static HashSet<Vector2Int> chunksMarkedToUnload;

        private void Awake() {
            if (Instance != null) {
                Destroy(gameObject);
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            loadedChunks = new();
            chunksMarkedToUnload = new();
        }


        /// <summary>
        /// Stop receiving chunk loader updates, unloading all chunks that should be unloaded.
        /// </summary>
        public static void UnloadChunksMarkedForUnloading() {
            foreach(Vector2Int coords in chunksMarkedToUnload) {
                Instance.loadedChunks[coords].Unload();
                Instance.loadedChunks.Remove(coords);
            }
            chunksMarkedToUnload.Clear();
        }


        /// <summary>
        /// Converts coordinates into a chunk. Optionally creates a chunk if it doesn't exist.
        /// </summary>
        /// <param name="coords"> The coordinates of the chunk to get </param>
        /// <param name="createIfNotExists"> Should a new chunk be created if it isn't found? </param>
        /// <returns></returns>
        public static WNetChunk GetChunk(Vector2Int coords, bool createIfNotExists = false) {
            if(Instance.loadedChunks.TryGetValue(coords, out WNetChunk chunk))
                return chunk;

            if(createIfNotExists) {
                WNetChunk newChunk = new();
                newChunk.Load(coords);
                Instance.loadedChunks.Add(coords, newChunk);
                return newChunk;
            }

            return null;
        }


        /// <summary>
        /// Removes a chunk loader (entity) from a chunk at a given position, and optionally, all its neighbors.
        /// </summary>
        /// <param name="coords"> The coordinates of the chunk to act on </param>
        /// <param name="entity"> The chunk loader </param>
        /// <param name="removeFromAllNearbyChunks"> Should this act on all nearby chunks? </param>
        public static void RemoveChunkLoader(Vector2Int coords, WNetEntity entity, bool removeFromAllNearbyChunks = true) {
            if(removeFromAllNearbyChunks) {
                foreach(Vector2Int offset in GetOffsets(coords)) {
                    RemoveChunkLoader(offset, entity, false);
                }
            }

            WNetChunk chunk = GetChunk(coords, false);
            if (chunk == null)
                return;

            chunk.RemoveChunkLoader(entity);
            Debug.Log($"Removed chunk loader from {coords}");
        }


        /// <summary>
        /// Adds a chunk loader (entity) to a chunk at a given position, and optionally, all its neighbors.
        /// </summary>
        /// <param name="coords"> The coordinates of the chunk to act on </param>
        /// <param name="entity"> The chunk loader </param>
        /// <param name="addToAllNearbyChunks"> Should this act on all nearby chunks? </param>
        public static void AddChunkLoader(Vector2Int coords, WNetEntity entity, bool addToAllNearbyChunks = false) {
            if(addToAllNearbyChunks) {
                foreach(Vector2Int offset in GetOffsets(coords)) {
                    AddChunkLoader(offset, entity, false);
                }
                return;
            }
            
            GetChunk(coords, true).AddChunkLoader(entity);
            Debug.Log($"Added chunk loader to {coords}");
        }


        /// <summary>
        /// Removes and adds a chunk loader from and to chunks based on starting and ending positions.
        /// Also moves the entity from the "from" chunk to the "to" chunk.
        /// </summary>
        /// <param name="entity"> The chunk loader </param>
        /// <param name="from"> From chunk coords </param>
        /// <param name="to"> To chunk coords </param>
        private static void MoveChunkLoader(WNetEntity entity, Vector2Int from, Vector2Int to) {
            Vector2Int[] startingLoadedChunks = GetOffsets(from);

            HashSet<Vector2Int> endingLoadedChunks = new();
            PutOffsetsInHashSet(to, endingLoadedChunks);

            for(int i=0; i<9; i++) {
                if (endingLoadedChunks.Contains(startingLoadedChunks[i])) {
                    // This chunk is in end and start coords
                    // Do nothing
                } else {
                    // This chunk is in start coords but not end coords
                    // Unload it
                    Debug.Log($"Unloaded chunk {startingLoadedChunks[i]}");
                    RemoveChunkLoader(startingLoadedChunks[i], entity);
                }
                endingLoadedChunks.Remove(startingLoadedChunks[i]);
            }

            foreach(var coords in endingLoadedChunks) {
                // This chunk is in end coords but not start coords
                // Load it
                Debug.Log($"Loading chunk {coords}");
                AddChunkLoader(coords, entity);
            }
        }


        /// <summary>
        /// Moves an entity from chunk "from" to chunk "to".
        /// </summary>
        /// <returns> The chunk the entity was moved to if it exists, or null. </returns>
        public static WNetChunk MoveEntityBetweenChunks(WNetEntity entity, Vector2Int from, Vector2Int to) {
            if(entity.IsChunkLoader) {
                MoveChunkLoader(entity, from, to);
            }

            WNetChunk fromChunk = GetChunk(from, false);
            fromChunk.PresentEntities.Remove(entity);
            WNetChunk toChunk = GetChunk(to, false);
            toChunk.PresentEntities.Add(entity);

            return toChunk;
        }


        public static readonly Vector2Int[] offsets = new Vector2Int[9] {
            new Vector2Int(-1,  1), new Vector2Int( 0,  1), new Vector2Int( 1,  1),
            new Vector2Int(-1,  0), new Vector2Int( 0,  0), new Vector2Int( 1,  0),
            new Vector2Int(-1, -1), new Vector2Int( 0, -1), new Vector2Int( 1, -1)
        };


        public static Vector2Int[] GetOffsets(Vector2Int center) {
            return new Vector2Int[9] {
                center + offsets[0], center + offsets[1], center + offsets[2],
                center + offsets[3], center + offsets[4], center + offsets[5],
                center + offsets[6], center + offsets[7], center + offsets[8]
            };
        }


        public static void PutOffsetsInHashSet(Vector2Int center, HashSet<Vector2Int> addTo) {
            for (int i = 0; i < 9; i++) {
                addTo.Add(center + offsets[i]);
            }
        }

        public static Vector2Int ProjectToGrid(Vector3 position) {
            Vector2Int ret = new Vector2Int(Mathf.RoundToInt(position.x / chunkSize), Mathf.RoundToInt(position.z / chunkSize));

            return ret;
        }
    }
}
