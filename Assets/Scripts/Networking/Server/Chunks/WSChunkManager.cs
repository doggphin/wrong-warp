using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using LiteNetLib.Utils;
using Networking.Shared;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem.Interactions;
using UnityEngine.UIElements;

namespace Networking.Server {
    public static class WSChunkManager {
        private const float chunkSize = 32;
        private static Dictionary<Vector2Int, WSChunk> loadedChunks = new();

        public static HashSet<Vector2Int> chunksMarkedToUnload = new();


        public static void ResetChunkUpdatesAndSnapshots() {
            foreach(WSChunk chunk in loadedChunks.Values) {
                chunk.ResetUpdates();
            }
        }


        public static WSEntitiesLoadedDeltaPkt GetEntitiesLoadedDeltaPkt(Vector2Int? from, Vector2Int to) {
            if(from == to)
                return null;

            Vector2Int[] leaving;
            Vector2Int[] entering;
            
            if(from != null) {
                GetChunkDifference((Vector2Int)from, to, out leaving, out entering);
            } else {
                leaving = new Vector2Int[0];
                entering = new Vector2Int[9];
                GetNeighboringChunks(to, false);
            }
            

            HashSet<int> entityIdsLeaving = new();
            HashSet<WEntitySerializable> entitiesEntering = new();

            if(leaving.Length > 0) {
                foreach(var coords in leaving) {
                    WSChunk chunkLeaving = GetChunk(coords, false);
                    
                    foreach(var entity in chunkLeaving.PresentEntities) {
                        entityIdsLeaving.Add(entity.Id);
                    }
                }
            }
            

            foreach(var coords in entering) {
                WSChunk chunkEntering = GetChunk(coords, false);

                foreach(var entity in chunkEntering.PresentEntities) {
                    entitiesEntering.Add(entity.GetSerializedEntity(WSNetServer.Tick));
                }
            }
            
            return new WSEntitiesLoadedDeltaPkt() {
                entityIdsToRemove = entityIdsLeaving.ToList(),
                entitiesToAdd = entitiesEntering.ToList()
            };
        }


        public static bool GetChunkDifference(Vector2Int from, Vector2Int to, out Vector2Int[] lost, out Vector2Int[] found) {
            if(from == to) {
                lost = null;
                found = null;
                return false;
            }
            
            HashSet<Vector2Int> fromCoordsSet = GetNeighboringChunksAsHashSet(from, true);
            HashSet<Vector2Int> toCoordsSet = GetNeighboringChunksAsHashSet(to, true);

            lost = fromCoordsSet.Except(toCoordsSet).ToArray();
            found = toCoordsSet.Except(fromCoordsSet).ToArray();

            return true;
        }



        /// <summary>
        /// Stop receiving chunk loader updates, unloading all chunks that should be unloaded.
        /// </summary>
        public static void UnloadChunksMarkedForUnloading() {
            foreach(Vector2Int coords in chunksMarkedToUnload) {
                loadedChunks[coords].Unload();
                loadedChunks.Remove(coords);
            }
            chunksMarkedToUnload.Clear();
        }


        /// <summary>
        /// Converts coordinates into a chunk. Optionally creates a chunk if it doesn't exist.
        /// </summary>
        /// <param name="coords"> The coordinates of the chunk to get </param>
        /// <param name="createIfNotExists"> Should a new chunk be created if it isn't found? </param>
        /// <returns></returns>
        public static WSChunk GetChunk(Vector2Int coords, bool createIfNotExists = false) {
            if(loadedChunks.TryGetValue(coords, out WSChunk chunk))
                return chunk;

            if(createIfNotExists) {
                WSChunk newChunk = new();
                newChunk.Load(coords);
                loadedChunks.Add(coords, newChunk);
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
        public static void RemoveChunkLoader(Vector2Int coords, WSEntity entity, bool removeFromAllNearbyChunks = false) {
            if(removeFromAllNearbyChunks) {
                foreach(Vector2Int offset in GetOffsets(coords)) {
                    RemoveChunkLoader(offset, entity, false);
                }
            }

            WSChunk chunk = GetChunk(coords, false);
            if (chunk == null)
                return;

            chunk.RemoveChunkLoader(entity);
            //Debug.Log($"Removed chunk loader from {coords}");
        }


        /// <summary>
        /// Adds a chunk loader (entity) to a chunk at a given position, and optionally, all its neighbors.
        /// </summary>
        /// <param name="coords"> The coordinates of the chunk to act on </param>
        /// <param name="entity"> The chunk loader </param>
        /// <param name="addToAllNearbyChunks"> Should this act on all nearby chunks? </param>
        public static void AddChunkLoader(Vector2Int coords, WSEntity entity, bool addToAllNearbyChunks = false) {
            if(addToAllNearbyChunks) {
                foreach(Vector2Int offset in GetOffsets(coords)) {
                    AddChunkLoader(offset, entity, false);
                }
                return;
            }
            
            GetChunk(coords, true).AddChunkLoader(entity);
            //Debug.Log($"Added chunk loader to {coords}");
        }


        /// <summary>
        /// Removes and adds a chunk loader from and to chunks based on starting and ending positions.
        /// Also moves the entity from the "from" chunk to the "to" chunk.
        /// </summary>
        /// <param name="entity"> The chunk loader </param>
        /// <param name="from"> From chunk coords </param>
        /// <param name="to"> To chunk coords </param>
        private static void MoveChunkLoader(WSEntity entity, Vector2Int from, Vector2Int to) {
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
                    RemoveChunkLoader(startingLoadedChunks[i], entity, false);
                }
                endingLoadedChunks.Remove(startingLoadedChunks[i]);
            }

            foreach(var coords in endingLoadedChunks) {
                // This chunk is in end coords but not start coords
                // Load it
                AddChunkLoader(coords, entity);
            }
        }


        /// <summary>
        /// Moves an entity from chunk "from" to chunk "to".
        /// </summary>
        /// <returns> The chunk the entity was moved to if it exists, or null. </returns>
        public static WSChunk MoveEntityBetweenChunks(WSEntity entity, Vector2Int from, Vector2Int to) {
            if (entity.IsChunkLoader) {
                MoveChunkLoader(entity, from, to);
            }

            WSChunk fromChunk = GetChunk(from, false);
            fromChunk.PresentEntities.Remove(entity);
            WSChunk toChunk = GetChunk(to, false);
            toChunk.PresentEntities.Add(entity);

            return toChunk;
        }


        public static Vector2[] GetNeighboringChunksAsArray(Vector2Int center, bool getCenter = true) {
            Vector2[] ret = new Vector2[getCenter ? 9 : 8];

            for (int offsetIndex = 0, bufferIndex = 0; offsetIndex < 9; offsetIndex++, bufferIndex++) {
                if (offsetIndex == 4 && !getCenter)
                    offsetIndex++;

                ret[bufferIndex] = offsets[offsetIndex];
            }

            return ret;
        }


        public static HashSet<Vector2Int> GetNeighboringChunksAsHashSet(Vector2Int center, bool getCenter = true) {
            HashSet<Vector2Int> ret = new(getCenter ? 9 : 8);

            for (int i = 0; i < 9; i++) {
                if (i == 4 && !getCenter)
                    i++;
                    
                ret.Add(center + offsets[i]);
            }

            return ret;
        }



        public static WSChunk[] GetNeighboringChunks(Vector2Int center, bool getCenter = true, bool createIfNotExists = false) {
            WSChunk[] ret = new WSChunk[getCenter ? 9 : 8];
            
            for (int offsetIndex = 0, retIndex = 0; offsetIndex < 9;) {
                if (offsetIndex == 4 && !getCenter) {
                    offsetIndex++;
                }
                    
                ret[retIndex++] = GetChunk(offsets[offsetIndex++] + center, createIfNotExists);
            }

            return ret;
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
