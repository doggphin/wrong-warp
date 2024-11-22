using LiteNetLib.Utils;
using UnityEngine;

using Networking.Shared;

namespace Networking.Server {
    public class WSEntity : WEntityBase {
        public bool updatePosition, updateRotation, updateScale;
        private Vector3 lastPosition;
        private Quaternion lastRotation;
        private Vector3 lastScale;
        public Vector2Int ChunkPosition { get; private set; }
        public WSChunk CurrentChunk { get; private set; } = null;

        private bool isChunkLoader;
        public bool IsChunkLoader {
            get {
                return isChunkLoader;
            }
            set {
                if (isChunkLoader && !value) {
                    WSChunkManager.RemoveChunkLoader(ChunkPosition, this, true);
                }
                else if (!isChunkLoader && value) {
                    WSChunkManager.AddChunkLoader(ChunkPosition, this, true);
                }
                
                isChunkLoader = value;
            }
        }

        private bool isSerialized = false;
        private WEntitySerializable serializedEntity = new();

        public WEntitySerializable GetSerializedEntity() {
            if(!isSerialized) {
                serializedEntity.entityId = Id;
                serializedEntity.prefabId = PrefabId;

                serializedEntity.transform.position = transform.position;
                serializedEntity.transform.rotation = transform.rotation;
                serializedEntity.transform.scale = transform.localScale;
            }
            
            isSerialized = true;
            return serializedEntity;
        }


        public void Init(int entityId, WPrefabId prefabId, bool isChunkLoader) {
            Id = entityId;
            PrefabId = prefabId;
            ChunkPosition = WSChunkManager.ProjectToGrid(transform.position);
            IsChunkLoader = isChunkLoader;
            CurrentChunk = WSChunkManager.GetChunk(ChunkPosition, false);
        }


        public void Poll(int tick) {
            if (!gameObject.activeInHierarchy || isDead)
                return;

            bool hasMoved = updatePosition && lastPosition != transform.position;
            bool hasRotated = updateRotation && lastRotation != transform.rotation;
            bool hasScaled = updateScale && lastScale != transform.localScale;

            WSEntityTransformUpdatePkt transformPacket = hasMoved || hasRotated || hasScaled ?
                new() {
                    transform = new WTransformSerializable() {
                        position = hasMoved ? transform.position : null,
                        rotation = hasRotated ? transform.rotation : null,
                        scale = hasScaled ? transform.localScale : null
                    }
                }
                : null;

            if (hasScaled)
                lastScale = transform.localScale;
            if (hasRotated)
                lastRotation = transform.rotation;
            if (hasMoved) {
                lastPosition = transform.position;
                Vector2Int newChunkPosition = WSChunkManager.ProjectToGrid(transform.position);
                if (newChunkPosition != ChunkPosition) {
                    CurrentChunk = WSChunkManager.MoveEntityBetweenChunks(this, ChunkPosition, newChunkPosition);
                }
                ChunkPosition = newChunkPosition;
            }

            if(transformPacket != null)
                PushUpdate(tick, transformPacket);
        }


        public override void Kill(WEntityKillReason killReason) {
            Debug.Log("Killing object!");
            switch(killReason) {
                default:
                    gameObject.SetActive(false);
                    break;
            }

            if (WNetManager.IsServer) {
                PushUpdate(WSNetServer.Tick, new WSEntityKillPkt() { reason = killReason });

                if (isChunkLoader) {
                    WSChunkManager.RemoveChunkLoader(CurrentChunk.Coords, this, true);
                }
            }

            isDead = true;
        }


        public void PushUpdate(int tick, INetSerializable packet) {
            isSerialized = false;

            if (isDead) {
                Debug.Log("I'm dead, I can't push an update out!");
                return;
            }

            CurrentChunk.AddEntityUpdate(tick, Id, packet);
        }
    }
}

