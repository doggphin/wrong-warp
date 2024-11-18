using System.Collections.Generic;
using UnityEngine;

namespace Networking.Shared {
    public class WNetChunkManager : MonoBehaviour {
        [SerializeField] private float rebuildTime = 5;
        [SerializeField] private float chunkSize = 20;

        private Dictionary<Vector3Int, WNetChunk> loadedChunks;

        private Vector3Int[] neighborOffsets = new Vector3Int[27];
        public Vector3Int ProjectToGrid(Vector3 position) => Vector3Int.RoundToInt(position / chunkSize);

        private void Awake() {
            loadedChunks = new();
            neighborOffsets = new Vector3Int[9 * 3];
            int i = 0;
            for (int x = -1; x <= 1; x++) {
                for (int y = -1; y <= 1; y++) {
                    for (int z = -1; z <= 1; z++) {
                        neighborOffsets[i] = new Vector3Int(x, y, z);
                        i += 1;
                    }
                }
            }
        }


        public WNetChunk GetChunk(Vector3Int position, bool createIfNotExists = false) {
            if(loadedChunks.TryGetValue(position, out WNetChunk chunk))
                return chunk;

            if(createIfNotExists) {
                WNetChunk newChunk = new();
                loadedChunks.Add(position, newChunk);
                return newChunk;
            }

            return null;
        }


        public void ReloadChunks() {
            // Get active cells based on the cells surrounding each player
            HashSet<Vector3Int> activeCells = new();
            foreach (var peer in WNetManager.Instance.WNetServer.ServerNetManager.ConnectedPeerList) {
                Vector3Int position = ProjectToGrid(((WNetPlayer)peer.Tag).transform.position);
                foreach(var offset in neighborOffsets) {
                    activeCells.Add(offset + position);
                }
            }

            // For every chunk, check if it should still be loaded
            foreach(var chunk in loadedChunks) {
                if(activeCells.Contains(chunk.Key)) {
                    // Chunk is already loaded. Keep it loaded
                    // This activeCell has been handled
                    activeCells.Remove(chunk.Key);
                } else {
                    // Chunk is no longer loaded. Unload it
                    loadedChunks.Remove(chunk.Key);
                    //chunk.Value.Unload();
                }
            }

            // Load all unhandled (nonexistent) cells as new chunks
            foreach(var cellToLoad in activeCells) {
                WNetChunk newChunk = new();
                //newChunk.Load();
                loadedChunks.Add(cellToLoad, newChunk);
            }
        }
    }
}
