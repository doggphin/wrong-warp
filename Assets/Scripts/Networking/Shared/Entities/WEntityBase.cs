
using UnityEngine;

namespace Networking.Shared {
    public abstract class WEntityBase : MonoBehaviour {
        public int Id { get; protected set; }
        public WPrefabId PrefabId { get; protected set; }
        protected bool isDead;
        
        public bool updatePositionsLocally, updateRotationsLocally, updateScalesLocally;

        public CircularTickBuffer<Vector3> positionsBuffer = new();
        public CircularTickBuffer<Quaternion> rotationsBuffer = new();
        public CircularTickBuffer<Vector3> scalesBuffer = new();

        private void Awake() {
            for(int i=0; i<WCommon.TICKS_PER_SECOND; i++) {
                positionsBuffer[i] = Vector3.zero;
                rotationsBuffer[i] = Quaternion.identity;
                scalesBuffer[i] = Vector3.one;
            }
        }

        public Vector3 LerpBufferedPositions(int toTick, float percentage) =>
            Vector3.Lerp(positionsBuffer[toTick - 1], positionsBuffer[toTick], percentage);
        public Quaternion LerpBufferedRotations(int startingFromTick, float percentage) =>
            Quaternion.Lerp(rotationsBuffer[startingFromTick - 1], rotationsBuffer[startingFromTick], percentage);   
        public Vector3 LerpBufferedScales(int toTick, float percentage) =>
            Vector3.Lerp(scalesBuffer[toTick - 1], scalesBuffer[toTick], percentage);

        public abstract void Kill(WEntityKillReason reason);
    }
}