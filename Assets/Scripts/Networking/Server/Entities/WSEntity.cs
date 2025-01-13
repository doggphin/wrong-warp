using LiteNetLib.Utils;
using UnityEngine;

using Networking.Shared;
using UnityEditor.UI;
using System;

namespace Networking.Server {
    public class WSEntity : WEntityBase {
        public bool updatePosition, updateRotation, updateScale;  

        public Vector2Int ChunkPosition { get; private set; }
        public WSChunk CurrentChunk { get; private set; } = null;

        public Action<WSEntity, WEntityKillReason> Killed;

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
        public WEntitySerializable GetSerializedEntity(int tick) {
            if(!isSerialized) {
                serializedEntity.entityId = Id;
                serializedEntity.prefabId = PrefabId;

                serializedEntity.transform = new WTransformSerializable {
                    position = positionsBuffer[tick],
                    rotation = rotationsBuffer[tick],
                    scale = scalesBuffer[tick]
                };
            }
            
            isSerialized = true;
            return serializedEntity;
        }


        public void Init(int entityId, WPrefabId prefabId, bool isChunkLoader) {
            Id = entityId;
            PrefabId = prefabId;
            ChunkPosition = WSChunkManager.ProjectToGrid(positionsBuffer[WSNetServer.Tick]);
            IsChunkLoader = isChunkLoader;
            CurrentChunk = WSChunkManager.GetChunk(ChunkPosition, false);
        }   


        private void Update() {
            float percentageThroughTick = WSNetServer.GetPercentageThroughTick();

            //if(!updatePositionsLocally)
                transform.position = LerpBufferedPositions(WSNetServer.Tick, percentageThroughTick);

            if(!updateRotationsLocally)  
                transform.rotation = LerpBufferedRotations(WSNetServer.Tick, percentageThroughTick);

            if(!updateScalesLocally)
                transform.localScale = LerpBufferedScales(WSNetServer.Tick, percentageThroughTick);
        }


        public void PollAndFinalizeTransform() {
            positionsBuffer[WSNetServer.Tick + 1] = positionsBuffer[WSNetServer.Tick];
            rotationsBuffer[WSNetServer.Tick + 1] = rotationsBuffer[WSNetServer.Tick];
            scalesBuffer[WSNetServer.Tick + 1] = scalesBuffer[WSNetServer.Tick];

            if (!gameObject.activeInHierarchy || isDead)
                return;

            bool hasMoved = positionsBuffer[WSNetServer.Tick] != positionsBuffer[WSNetServer.Tick - 1];
            bool hasRotated = rotationsBuffer[WSNetServer.Tick] != rotationsBuffer[WSNetServer.Tick - 1];
            bool hasScaled = scalesBuffer[WSNetServer.Tick] != scalesBuffer[WSNetServer.Tick - 1];

            if(!hasMoved && !hasRotated && !hasScaled)
                return;

            WSEntityTransformUpdatePkt transformPacket = new() {
                transform = new WTransformSerializable() {
                    position = hasMoved ? positionsBuffer[WSNetServer.Tick] : null,
                    rotation = hasRotated ? rotationsBuffer[WSNetServer.Tick] : null,
                    scale = hasScaled ? scalesBuffer[WSNetServer.Tick] : null
                }
            };

            if (hasMoved) {
                Vector2Int newChunkPosition = WSChunkManager.ProjectToGrid(positionsBuffer[WSNetServer.Tick]);
        
                if (newChunkPosition != ChunkPosition)
                    CurrentChunk = WSChunkManager.MoveEntityBetweenChunks(this, ChunkPosition, newChunkPosition);

                ChunkPosition = newChunkPosition;
            } 

            if(transformPacket != null)
                PushUpdates(transformPacket);
        }


        // This should be called from the entity manager.
        public override void Kill(WEntityKillReason killReason) {
            isDead = true;
            Killed?.Invoke(this, killReason);

            switch(killReason) {
                // Start a coroutine playing death animation if dying maybe?
                // Or just spawn an entity to play the death animation/ragdoll?
                default:
                    Destroy(gameObject);
                    break;
            }
        }


        private void PushUpdates(INetSerializable packet) {
            isSerialized = false;

            if (isDead) {
                Debug.Log("I'm dead, I can't push an update out!");
                return;
            }

            CurrentChunk.AddEntityUpdate(WSNetServer.Tick, Id, packet);
        }
    }
}

