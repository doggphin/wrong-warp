using LiteNetLib.Utils;
using UnityEngine;

using Networking.Shared;
using UnityEditor.UI;
using System;

namespace Networking.Server {
    public class SEntity : BaseEntity {
        public bool updatePositionOverNetwork, updateRotationOverNetwork, updateScaleOverNetwork;
        public bool isRigidbody;

        public SChunk Chunk { get; set; }

        public Action<SEntity> FinishedDying;
        public static Action<SEntity, SPlayer> SetAsPlayer;
        public static Action<SEntity, SPlayer> UnsetAsPlayer;

        public Action<SEntity, BasePacket> PushUnreliableUpdate;

        private SPlayer player;
        public bool IsPlayer => player != null;
        public SPlayer Player => player;


        public WEntitySerializable GetSerializedEntity(int tick) {
            return new WEntitySerializable() {
                entityId = Id,
                entityPrefabId = PrefabId,
                transform = new TransformSerializable {
                    position = positionsBuffer[tick],
                    rotation = rotationsBuffer[tick],
                    scale = scalesBuffer[tick]
                }
            };
        }


        public void Init(int entityId, EntityPrefabId prefabId, Vector3 position, Quaternion rotation, Vector3 scale) {
            Id = entityId;
            PrefabId = prefabId;
            
            SetPosition(position, true);
            SetRotation(rotation, true);
            SetScale(scale, true);
        }


        ///<summary> This should only ever be called from WSPlayer </summary>
        public void ChangePlayer(SPlayer newPlayer) {
            SPlayer currentPlayer = player;
            
            if(ReferenceEquals(player, newPlayer)) {
                return;
            }

            currentPlayer?.HandleSetEntity(null);
            newPlayer?.HandleSetEntity(this);
            
            player = newPlayer;
            
            if(currentPlayer != null && newPlayer == null) {
                UnsetAsPlayer?.Invoke(this, currentPlayer);
            }
            else if(currentPlayer == null && newPlayer != null) {
                SetAsPlayer?.Invoke(this, newPlayer);
            }
        }


        void Update() {
            float percentageThroughTick = SNetManager.Instance.GetPercentageThroughTick();
            int tick = SNetManager.Instance.GetTick();

            if(setVisualPositionAutomatically)
                transform.position = LerpBufferedPositions(tick, percentageThroughTick);

            if(setVisualRotationAutomatically)  
                transform.rotation = LerpBufferedRotations(tick, percentageThroughTick);

            if(setVisualScaleAutomatically)
                transform.localScale = LerpBufferedScales(tick, percentageThroughTick);
        }

        public void PollAndFinalizeTransform() {
            int tick = SNetManager.Instance.GetTick();
            int previousTick = tick - 1;
            int futureTick = tick + 1;

            if(isRigidbody) {
                positionsBuffer[tick] = transform.position;
                rotationsBuffer[tick] = transform.rotation;
            } else {
                CopyTransformToNextTick(tick);
            }

            if (!gameObject.activeInHierarchy || isDead)
                return;

            bool hasMoved = positionsBuffer[tick] != positionsBuffer[previousTick];
            bool hasRotated = rotationsBuffer[tick] != rotationsBuffer[previousTick];
            bool hasScaled = scalesBuffer[tick] != scalesBuffer[previousTick];

            if(!hasMoved && !hasRotated && !hasScaled)
                return;

            SEntityTransformUpdatePkt transformPacket = new() {
                transform = new TransformSerializable() {
                    position = hasMoved ? positionsBuffer[tick] : null,
                    rotation = hasRotated ? rotationsBuffer[tick] : null,
                    scale = hasScaled ? scalesBuffer[tick] : null
                }
            };

            if(transformPacket != null) {
                PushUnreliableUpdate?.Invoke(this, transformPacket);
            }     
        }


        public override void StartDeath(WEntityKillReason reason) {
            if(isDead)
                return;
            
            isDead = true;

            switch(reason) {
                // Start a coroutine playing death animation if dying maybe?
                // Or just spawn an entity to play the death animation/ragdoll?
                // Decide that here
                default:
                    break;
            }

            // Push an update to show visual death, then start a coroutine here or something?? Override "StartDeath" on different entity types

            FinishDeath();
        }


        private void FinishDeath() {
            FinishedDying?.Invoke(this);
        }

        
        void OnDestroy() {
            if(!isDead) {
                FinishedDying?.Invoke(this);
            }
        }
    }
}

