using LiteNetLib.Utils;
using UnityEngine;

using Networking.Shared;
using UnityEditor.UI;
using System;

namespace Networking.Server {
    public class SEntity : BaseEntity {
        public bool updatePosition, updateRotation, updateScale;  

        ///<summary> Will be null if this is not attached to a player. </summary>
        public SPlayer Player { get; private set; }
        public Vector2Int ChunkPosition { get; private set; }
        public SChunk CurrentChunk { get; private set; } = null;

        public Action<SEntity, WEntityKillReason> Killed;
        public static Action<SEntity> SetAsChunkLoader;
        public static Action<SEntity> RemoveChunkLoader;

        private bool isChunkLoader;
        public bool IsChunkLoader {
            get {
                return isChunkLoader;
            }
            set {
                if (isChunkLoader && !value) {
                    RemoveChunkLoader?.Invoke(this);
                    //SChunkManager.RemoveChunkLoader(ChunkPosition, this, true);
                }
                else if (!isChunkLoader && value) {
                    SetAsChunkLoader?.Invoke(this);
                    //SChunkManager.AddChunkLoader(ChunkPosition, this, true);
                }
                
                isChunkLoader = value;
            }
        }

        private bool isSerialized = false;
        private WEntitySerializable serializedEntity = new();
        public WEntitySerializable GetSerializedEntity(int tick) {
            if(!isSerialized) {
                serializedEntity.entityId = Id;
                serializedEntity.entityPrefabId = PrefabId;

                serializedEntity.transform = new TransformSerializable {
                    position = positionsBuffer[tick],
                    rotation = rotationsBuffer[tick],
                    scale = scalesBuffer[tick]
                };
            }
            
            isSerialized = true;
            return serializedEntity;
        }


        public void Init(int entityId, EntityPrefabId prefabId, bool isChunkLoader) {
            Id = entityId;
            PrefabId = prefabId;
            ChunkPosition = SChunkManager.ProjectToGrid(positionsBuffer[SNetManager.Instance.GetTick()]);
            CurrentChunk = SChunkManager.GetChunk(ChunkPosition, true);
            IsChunkLoader = isChunkLoader;
        }

        ///<summary> This should only ever be called from WSPlayer </summary>
        public void SetPlayer(SPlayer player) {
            if(Player != null && player != null)
                throw new Exception("Cannot set an entity's player without unsetting player first! Call this from WSPlayer.SetEntity!");
            
            Player = player;
        }


        private void Update() {
            float percentageThroughTick = SNetManager.Instance.GetPercentageThroughTick();
            int tick = SNetManager.Instance.GetTick();

            //if(!updatePositionsLocally)
                transform.position = LerpBufferedPositions(tick, percentageThroughTick);

            if(!updateRotationsLocally)  
                transform.rotation = LerpBufferedRotations(tick, percentageThroughTick);

            if(!updateScalesLocally)
                transform.localScale = LerpBufferedScales(tick, percentageThroughTick);
        }


        public void PollAndFinalizeTransform() {
            int tick = SNetManager.Instance.GetTick();
            int previousTick = tick - 1;
            int futureTick = tick + 1;

            positionsBuffer[futureTick] = positionsBuffer[tick];
            rotationsBuffer[futureTick] = rotationsBuffer[tick];
            scalesBuffer[futureTick] = scalesBuffer[tick];

            if (!gameObject.activeInHierarchy || isDead)
                return;

            bool hasMoved = positionsBuffer[tick] != positionsBuffer[previousTick];
            bool hasRotated = rotationsBuffer[tick] != rotationsBuffer[previousTick];
            bool hasScaled = scalesBuffer[tick] != scalesBuffer[previousTick];

            if(!hasMoved && !hasRotated && !hasScaled)
                return;

            WSEntityTransformUpdatePkt transformPacket = new() {
                transform = new TransformSerializable() {
                    position = hasMoved ? positionsBuffer[tick] : null,
                    rotation = hasRotated ? rotationsBuffer[tick] : null,
                    scale = hasScaled ? scalesBuffer[tick] : null
                }
            };

            if (hasMoved) {
                Vector2Int newChunkPosition = SChunkManager.ProjectToGrid(positionsBuffer[tick]);
        
                if (newChunkPosition != ChunkPosition)
                    CurrentChunk = SChunkManager.MoveEntityBetweenChunks(this, ChunkPosition, newChunkPosition);

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


        private void PushUpdates(BasePacket packet) {
            isSerialized = false;

            if (isDead) {
                Debug.Log("I'm dead, I can't push an update out!");
                return;
            }

            CurrentChunk.AddEntityUpdate(SNetManager.Instance.GetTick(), Id, packet);
        }
    }
}

