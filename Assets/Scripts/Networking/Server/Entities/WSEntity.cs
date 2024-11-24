using LiteNetLib.Utils;
using UnityEngine;

using Networking.Shared;
using UnityEditor.Rendering;

namespace Networking.Server {
    public class WSEntity : WEntityBase {
        public bool updatePosition, updateRotation, updateScale;   

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

                serializedEntity.transform = new WTransformSerializable {
                    position = currentPosition,
                    rotation = currentRotation,
                    scale = currentScale
                };
            }
            
            isSerialized = true;
            return serializedEntity;
        }


        public void Init(int entityId, WPrefabId prefabId, bool isChunkLoader) {
            Id = entityId;
            PrefabId = prefabId;
            ChunkPosition = WSChunkManager.ProjectToGrid(currentPosition);
            IsChunkLoader = isChunkLoader;
            CurrentChunk = WSChunkManager.GetChunk(ChunkPosition, false);
        }


        private void Update() {
            float percentageThroughCurrentFrame = WCommon.GetPercentageTimeThroughCurrentTick();
            
            if(!renderPersonalPositionUpdates)
                transform.position = previousPosition + ((currentPosition - previousPosition) * percentageThroughCurrentFrame);

            if(!renderPersonalRotationUpdates)  
                transform.rotation = Quaternion.Lerp(previousRotation, currentRotation, percentageThroughCurrentFrame);

            if(!renderPersonalScaleUpdates)
                transform.localScale = previousScale + ((currentScale - previousScale) * percentageThroughCurrentFrame);
        }

        public void Poll(int tick) {
            if (!gameObject.activeInHierarchy || isDead)
                return;

            bool hasMoved = HasMoved;
            bool hasRotated = HasRotated;
            bool hasScaled = HasScaled;

            if(!hasMoved && !hasRotated && !hasScaled)
                return;

            WSEntityTransformUpdatePkt transformPacket = new() {
                transform = new WTransformSerializable() {
                    position = hasMoved ? currentPosition : null,
                    rotation = hasRotated ? currentRotation : null,
                    scale = hasScaled ? currentScale : null
                }
            };

            if (hasMoved) {
                previousPosition = currentPosition;

                Vector2Int newChunkPosition = WSChunkManager.ProjectToGrid(currentPosition);
                if (newChunkPosition != ChunkPosition)
                    CurrentChunk = WSChunkManager.MoveEntityBetweenChunks(this, ChunkPosition, newChunkPosition);
                ChunkPosition = newChunkPosition;
            }

            if (hasScaled)
                previousScale = currentScale;

            if (hasRotated)
                previousRotation = currentRotation;

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

            PushUpdate(WSNetServer.Tick, new WSEntityKillPkt() { reason = killReason });
            isDead = true;

            if (isChunkLoader)
                WSChunkManager.RemoveChunkLoader(CurrentChunk.Coords, this, true);
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

