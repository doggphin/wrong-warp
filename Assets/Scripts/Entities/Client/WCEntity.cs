using UnityEngine;
using Networking.Shared;
using UnityEngine.Timeline;
using UnityEngine.UIElements.Experimental;
using System;

namespace Networking.Client {
    public class WCEntity : EntityBase {
        public TimestampedCircularTickBuffer<Vector3> receivedPositions = new();
        public TimestampedCircularTickBuffer<Quaternion> receivedRotations = new();
        public TimestampedCircularTickBuffer<Vector3> receivedScales = new();

        public bool isMyPlayer;

        ///<summary> Tries to lerp a given transform value (position, rotation, or scale) between its values in the past and current times. </summary>
        private T? SetObservedTransformValue<T>(bool updateLocally, TimestampedCircularTickBuffer<T> receivedTransformValues, Func<T, T, float, T> lerpFunction, float percentageThroughTick) where T : struct {
            if(updateLocally || !receivedTransformValues.TryGetByTimestamp(WCNetClient.ObservingTick - 1, out T currentTransformValue))
                return null;

            if(receivedTransformValues.TryGetByTimestamp(WCNetClient.ObservingTick - 2, out T previousTransformValue))
                return lerpFunction(previousTransformValue, currentTransformValue, percentageThroughTick);
            
            return currentTransformValue;
        }

        void Update() {
            float percentageThroughTick = WCNetClient.Instance.GetPercentageThroughTick();

            // Non-player entities use ObservingTick
            if(!isMyPlayer) {
                SetObservedTransformValue(updatePositionsLocally, receivedPositions, Vector3.Lerp, percentageThroughTick);
                SetObservedTransformValue(updateRotationsLocally, receivedRotations, Quaternion.Lerp, percentageThroughTick);
                SetObservedTransformValue(updateScalesLocally, receivedScales, Vector3.Lerp, percentageThroughTick);
            }

            else {
                transform.position = LerpBufferedPositions(WCNetClient.SendingTick - 1, percentageThroughTick);
                // Never do rotations. These are always handled by the client
                transform.localScale = LerpBufferedScales(WCNetClient.SendingTick - 1, percentageThroughTick);
            }
        }

        ///<summary> Caches a serialized transform if it's newer than the currently cached serialized transform for the given tick </summary>
        public void SetTransformForTick(int tick, WTransformSerializable serializedTransform) {
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


        public void Init(WSEntitySpawnPkt spawnPkt) {
            Id = spawnPkt.entity.entityId;

            positionsBuffer[WCNetClient.ObservingTick] = spawnPkt.entity.transform.position.GetValueOrDefault(Vector3.zero);
            rotationsBuffer[WCNetClient.ObservingTick] = spawnPkt.entity.transform.rotation.GetValueOrDefault(Quaternion.identity);
            scalesBuffer[WCNetClient.ObservingTick] = spawnPkt.entity.transform.scale.GetValueOrDefault(Vector3.one);
        }

        public override void Kill(WEntityKillReason reason) {
            Debug.Log("RIP!!!!!");
            Destroy(gameObject);
        }
    }
}