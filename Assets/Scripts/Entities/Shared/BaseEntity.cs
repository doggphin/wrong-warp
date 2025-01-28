
using System;
using UnityEngine;

namespace Networking.Shared {
    [RequireComponent(typeof(EntityTags))]
    public abstract class BaseEntity : MonoBehaviour {
        public int Id { get; protected set; }
        public EntityPrefabId PrefabId { get; protected set; }
        protected bool isDead;
        
        public bool setVisualPositionAutomatically = true;
        public bool setVisualRotationAutomatically = true;
        public bool setVisualScaleAutomatically = true;

        public CircularTickBuffer<Vector3> positionsBuffer = new();
        public CircularTickBuffer<Quaternion> rotationsBuffer = new();
        public CircularTickBuffer<Vector3> scalesBuffer = new();

        private void Awake() {
            for(int i=0; i<NetCommon.TICKS_PER_SECOND; i++) {
                positionsBuffer[i] = Vector3.zero;
                rotationsBuffer[i] = Quaternion.identity;
                scalesBuffer[i] = Vector3.one;
            }
        }


        protected void CopyTransformToNextTick(int tick) {
            int nextTick = tick + 1;
            positionsBuffer[nextTick] = positionsBuffer[tick];
            rotationsBuffer[nextTick] = rotationsBuffer[tick];
            scalesBuffer[nextTick] = scalesBuffer[tick];
        }  


        private void SetTransformValue<T>
        (T transformValue, CircularTickBuffer<T> transformBuffer, bool copyToNextTick = false, int? tickOrDefault = null) {
            int tick = tickOrDefault.GetValueOrDefault(WWNetManager.GetTick());
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


        public Vector3 GetPosition(int tick) => positionsBuffer[tick];
        public Quaternion GetRotation(int tick) => rotationsBuffer[tick];
        public Vector3 GetScale(int tick) => scalesBuffer[tick];


        ///<summary> Lerps percentage way between the transform value on the tick before endTick and endTick </summary>
        private T LerpTransformValues<T>(Func<T, T, float, T> lerpFunction, CircularTickBuffer<T> transformValueBuffer, int endTick, float percentage) =>
            lerpFunction(transformValueBuffer[endTick - 1], transformValueBuffer[endTick], percentage);

        public Vector3 LerpBufferedPositions(int endTick, float percentage) => LerpTransformValues(Vector3.Lerp, positionsBuffer, endTick, percentage);
        public Quaternion LerpBufferedRotations(int endTick, float percentage) => LerpTransformValues(Quaternion.Lerp, rotationsBuffer, endTick, percentage);
        public Vector3 LerpBufferedScales(int endTick, float percentage) => LerpTransformValues(Vector3.Lerp, scalesBuffer, endTick, percentage);

        public abstract void StartDeath(WEntityKillReason reason);
    }
}