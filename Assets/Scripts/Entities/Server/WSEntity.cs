using LiteNetLib.Utils;
using UnityEngine;

using Networking.Shared;
using UnityEditor.UI;
using System;

namespace Networking.Server {
    public class WSEntity : EntityBase {
        public bool updatePosition, updateRotation, updateScale;  

        ///<summary> Will be null if this is not attached to a player. </summary>
        public WSPlayer Player { get; private set; }
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


        public void Init(int entityId, NetPrefabType prefabId, bool isChunkLoader) {
            Id = entityId;
            PrefabId = prefabId;
            ChunkPosition = WSChunkManager.ProjectToGrid(positionsBuffer[WSNetServer.Instance.GetTick()]);
            IsChunkLoader = isChunkLoader;
            CurrentChunk = WSChunkManager.GetChunk(ChunkPosition, false);
        }

        ///<summary> This should only ever be called from WSPlayer </summary>
        public void SetPlayer(WSPlayer player) {
            if(Player != null && player != null)
                throw new Exception("Cannot set an entity's player without unsetting player first! Call this from WSPlayer.SetEntity!");
            
            Player = player;
        }


        private void Update() {
            float percentageThroughTick = WSNetServer.Instance.GetPercentageThroughTick();
            int tick = WSNetServer.Instance.GetTick();

            //if(!updatePositionsLocally)
                transform.position = LerpBufferedPositions(tick, percentageThroughTick);

            if(!updateRotationsLocally)  
                transform.rotation = LerpBufferedRotations(tick, percentageThroughTick);

            if(!updateScalesLocally)
                transform.localScale = LerpBufferedScales(tick, percentageThroughTick);
        }


        public void PollAndFinalizeTransform() {
            int tick = WSNetServer.Instance.GetTick();
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
                transform = new WTransformSerializable() {
                    position = hasMoved ? positionsBuffer[tick] : null,
                    rotation = hasRotated ? rotationsBuffer[tick] : null,
                    scale = hasScaled ? scalesBuffer[tick] : null
                }
            };

            if (hasMoved) {
                Vector2Int newChunkPosition = WSChunkManager.ProjectToGrid(positionsBuffer[tick]);
        
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


        private void PushUpdates(NetPacketForClient packet) {
            isSerialized = false;

            if (isDead) {
                Debug.Log("I'm dead, I can't push an update out!");
                return;
            }

            CurrentChunk.AddEntityUpdate(WSNetServer.Instance.GetTick(), Id, packet);
        }
    }
}

