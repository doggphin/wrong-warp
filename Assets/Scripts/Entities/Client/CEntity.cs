using UnityEngine;
using Networking.Shared;
using UnityEngine.Timeline;
using UnityEngine.UIElements.Experimental;
using System;

namespace Networking.Client {
    public class CEntity : BaseEntity {
        public TimestampedCircularTickBuffer<Vector3> receivedPositions = new();
        public TimestampedCircularTickBuffer<Quaternion> receivedRotations = new();
        public TimestampedCircularTickBuffer<Vector3> receivedScales = new();

        Vector3 lastReceivedPosition = Vector3.zero;
        Quaternion lastReceivedRotation = Quaternion.identity;
        Vector3 lastReceivedScale = Vector3.one;
        void Update() {
            ///<summary> Given a tick, try to lerp a given transform value (position, rotation, or scale) between its current value and a previous value. </summary>
            ///<returns> Whether the position of the object changed </returns>
            static bool TryGetObservedTransformValue<T>(
            bool setTransformValueAutomatically, TimestampedCircularTickBuffer<T> receivedTransformValues, Func<T, T, float, T> lerpFunction,
            ref T lastReceivedTransformValue, float percentageThroughTick, out T outTransformValue)
            where T : struct {
                // If this entity is updated locally 
                if(!setTransformValueAutomatically) {
                    outTransformValue = default;
                    return false;
                }

                // If received a transform value this tick, lerp between a previous value and the current value
                if(receivedTransformValues.TryGetByTimestamp(CNetManager.ObservingTick - 1, out T currentTransformValue)) {
                    // Try to use the value from the previous tick if possible
                    if(receivedTransformValues.TryGetByTimestamp(CNetManager.ObservingTick - 2, out T previousTransformValue)) {
                        outTransformValue = lerpFunction(previousTransformValue, currentTransformValue, percentageThroughTick);
                        lastReceivedTransformValue = previousTransformValue;
                    // If it doesn't exist, use the last received value
                    } else {
                        outTransformValue = lerpFunction(lastReceivedTransformValue, currentTransformValue, percentageThroughTick);
                    }                
                } else {
                    // If didn't receive a transform value the last tick either, set transform value to the last received value
                    // This is done because lerping between past and current values will get close to the last transform value, but not actually reach it
                    // This fixes that
                    if(receivedTransformValues.TryGetByTimestamp(CNetManager.ObservingTick - 2, out T previousTransformValue)) {
                        lastReceivedTransformValue = previousTransformValue;
                    }

                    outTransformValue = lastReceivedTransformValue;
                }

                // As a low-hanging optimization in the future, consider finding places where false can be returned instead
                return true;
            }

            float percentageThroughTick = CNetManager.Instance.GetPercentageThroughTick();
            // Use different logic for player and non-player entities
            if(!ReferenceEquals(CNetManager.PlayerEntity, this)) {
                // For non-player entities --
                if(TryGetObservedTransformValue(setVisualPositionAutomatically, receivedPositions, Vector3.Lerp, ref lastReceivedPosition, percentageThroughTick, out var position))
                    transform.position = position;
                if(TryGetObservedTransformValue(setVisualRotationAutomatically, receivedRotations, Quaternion.Lerp, ref lastReceivedRotation, percentageThroughTick, out var rotation))
                    transform.rotation = rotation;
                if(TryGetObservedTransformValue(setVisualScaleAutomatically, receivedScales, Vector3.Lerp, ref lastReceivedScale, percentageThroughTick, out var scale))
                    transform.localScale = scale;
            } else {
                // For the main player --
                transform.position = LerpBufferedPositions(CNetManager.SendingTick - 1, percentageThroughTick);
                //        rotations are always set within player controllers, ignore it here
                transform.localScale = LerpBufferedScales(CNetManager.SendingTick - 1, percentageThroughTick);
            }
        }

        ///<summary> Caches a serialized transform if it's newer than the currently cached serialized transform for the given tick </summary>
        public void SetTransformForTick(int tick, TransformSerializable serializedTransform) {
            void SetTransformValueIfNotNull<T>(T? value, CircularTickBuffer<T> buffer, TimestampedCircularTickBuffer<T> timeststampBuffer) where T : struct {
                if(value.HasValue) {
                    buffer[tick] = value.Value;
                    timeststampBuffer.SetValueAndTimestamp(value.Value, tick);
                }
            }

            SetTransformValueIfNotNull(serializedTransform.position, positionsBuffer, receivedPositions);
            SetTransformValueIfNotNull(serializedTransform.rotation, rotationsBuffer, receivedRotations);
            SetTransformValueIfNotNull(serializedTransform.scale, scalesBuffer, receivedScales);
        }

        public void Init(SEntitySpawnPkt spawnPkt) {
            Id = spawnPkt.entity.entityId;
            
            lastReceivedPosition = positionsBuffer[CNetManager.ObservingTick] = spawnPkt.entity.transform.position ?? Vector3.zero;
            lastReceivedRotation = rotationsBuffer[CNetManager.ObservingTick] = spawnPkt.entity.transform.rotation ?? Quaternion.identity;
            lastReceivedScale =    scalesBuffer[CNetManager.ObservingTick]    = spawnPkt.entity.transform.scale ?? Vector3.one;
        }

        public override void StartDeath(WEntityKillReason reason) {
            Debug.Log("RIP!!!!!");
            Destroy(gameObject);
        }
    }
}