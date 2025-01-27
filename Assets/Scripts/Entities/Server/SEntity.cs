using LiteNetLib.Utils;
using UnityEngine;

using Networking.Shared;
using UnityEditor.UI;
using System;

namespace Networking.Server {
    public class SEntity : BaseEntity {
        public bool updatePositionOverNetwork, updateRotationOverNetwork, updateScaleOverNetwork;
        public bool isRigidbody;

        public NewSChunk Chunk { get; set; }

        public Action<SEntity> FinishedDying;
        public static Action<SEntity, SPlayer> SetAsPlayer;
        public static Action<SEntity, SPlayer> UnsetAsPlayer;

        public Action<SEntity, BasePacket> PushUnreliableUpdate;

        private SPlayer player;
        public bool IsPlayer => player != null;
        public SPlayer Player {
            get => player; 
            set {
                if(IsPlayer && value == null) {
                    UnsetAsPlayer?.Invoke(this, player);
                }
                else if(!IsPlayer && value != null) {
                    SetAsPlayer?.Invoke(this, value);
                }

                player = value;
            }
        }


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


        public void Init(int entityId, EntityPrefabId prefabId, SPlayer player, Vector3 position, Quaternion rotation, Vector3 scale) {
            Id = entityId;
            PrefabId = prefabId;
            
            SetPosition(position, true);
            SetRotation(rotation, true);
            SetScale(position, true);

            this.player = player;
        }


        ///<summary> This should only ever be called from WSPlayer </summary>
        public void SetPlayer(SPlayer player) {
            if(Player != null && player != null)
                throw new Exception("Cannot set an entity's player without unsetting player first! Call this from WSPlayer.SetEntity!");
            
            Player = player;
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

        
        private void CopyTransformToNextTick(int tick) {
            int nextTick = tick + 1;
            positionsBuffer[nextTick] = positionsBuffer[tick];
            rotationsBuffer[nextTick] = rotationsBuffer[tick];
            scalesBuffer[nextTick] = scalesBuffer[tick];
        }

        private void SetTransformValue<T>
        (T transformValue, CircularTickBuffer<T> transformBuffer, bool copyToNextTick = false, int? tickOrDefault = null) {
            int tick = tickOrDefault.GetValueOrDefault(SNetManager.Tick);
            transformBuffer[tick] = transformValue;
            if(copyToNextTick)
                transformBuffer[tick + 1] = transformValue;
        }

        public void SetPosition(Vector3 position, bool copyToNextTick = false, int? tickOrDefault = null) =>
            SetTransformValue(position, positionsBuffer, copyToNextTick, tickOrDefault);
        public void SetRotation(Quaternion rotation, bool copyToNextTick = false, int? tickOrDefault = null) =>
            SetTransformValue(rotation, rotationsBuffer, copyToNextTick, tickOrDefault);
        public void SetScale(Vector3 scale, bool copyToNextTick = false, int? tickOrDefault = null) =>
            SetTransformValue(scale, scalesBuffer, copyToNextTick, tickOrDefault);

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

            if(transformPacket != null)
                PushUnreliableUpdate?.Invoke(this, transformPacket);
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
                Debug.LogError("Destroyed an entity without letting it die first!");
            }
        }
    }
}

