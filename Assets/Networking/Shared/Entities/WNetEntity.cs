using LiteNetLib.Utils;
using Networking.Server;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ProBuilder.MeshOperations;

namespace Networking.Shared {
    public class WNetEntity : MonoBehaviour {
        public int Id { get; private set; }
        public WNetPrefabId PrefabId { get; private set; }

        [SerializeField]
        private bool updatePos, updateRot, updateScale;
        private Vector3 lastPosition;
        private Quaternion lastRotation;
        private Vector3 lastScale;
        public Vector2Int ChunkPosition { get; private set; } = Vector2Int.zero;
        public WNetChunk CurrentChunk { get; private set; } = null;
        private bool isDead;

        private bool isChunkLoader;
        public bool IsChunkLoader {
            get {
                return isChunkLoader;
            }
            set {
                if (isChunkLoader && !value) {
                    WNetChunkManager.RemoveChunkLoader(ChunkPosition, this, true);
                }
                else if (!isChunkLoader && value) {
                    WNetChunkManager.AddChunkLoader(ChunkPosition, this, true);
                }
                
                isChunkLoader = value;
            }
        }


        public void InitServer(int id, bool isChunkLoader) {
            this.Id = id;
            ChunkPosition = WNetChunkManager.ProjectToGrid(transform.position);
            IsChunkLoader = isChunkLoader;
            CurrentChunk = WNetChunkManager.GetChunk(ChunkPosition, false);
        }


        public void Poll(int tick) {
            if (!gameObject.activeInHierarchy || isDead)
                return;

            bool hasMoved = updatePos && lastPosition != transform.position;
            bool hasRotated = updateRot && lastRotation != transform.rotation;
            bool hasScaled = updateScale && lastScale != transform.localScale;

            WSEntityTransformUpdatePkt transformPacket = hasMoved || hasRotated || hasScaled ?
                new() {
                    position = hasMoved ? transform.position : null,
                    rotation = hasRotated ? transform.rotation : null,
                    scale = hasScaled ? transform.localScale : null
                }
                : null;

            if (hasScaled)
                lastScale = transform.localScale;
            if (hasRotated)
                lastRotation = transform.rotation;
            if (hasMoved) {
                lastPosition = transform.position;

                Vector2Int newChunkPosition = WNetChunkManager.ProjectToGrid(transform.position);
                if (newChunkPosition != ChunkPosition) {
                    CurrentChunk = WNetChunkManager.MoveEntityBetweenChunks(this, ChunkPosition, newChunkPosition);
                }
                ChunkPosition = newChunkPosition;
            }

            if(transformPacket != null)
                PushUpdate(tick, transformPacket);
        }


        public void Kill(WEntityKillReason killReason) {
            Debug.Log("Killing object!");
            switch(killReason) {
                default:
                    gameObject.SetActive(false);
                    break;
            }

            if (WNetManager.IsServer) {
                PushUpdate(WNetServer.Instance.Tick, new WSEntityKillUpdatePkt() { killReason = killReason });

                if (isChunkLoader) {
                    WNetChunkManager.RemoveChunkLoader(CurrentChunk.Coords, this, true);
                }
            }

            isDead = true;
        }


        public void PushUpdate(int tick, INetSerializable packet) {
            if (isDead) {
                Debug.Log("I'm dead, I can't push an update out!");
                return;
            }

            CurrentChunk.AddEntityUpdate(tick, Id, packet);
        }
    }
}

