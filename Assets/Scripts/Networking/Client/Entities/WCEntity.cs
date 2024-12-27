using UnityEngine;
using Networking.Shared;

namespace Networking.Client {
    public class WCEntity : WEntityBase {
        public TimestampedCircularTickBuffer<Vector3> receivedPositions = new();
        public TimestampedCircularTickBuffer<Quaternion> receivedRotations = new();
        public TimestampedCircularTickBuffer<Vector3> receivedScales = new();


        public bool isMyPlayer;

        private void Update() {
            float percentageThroughTick = WCNetClient.PercentageThroughTick;

            // Non-player entities use ObservingTick
            if(!isMyPlayer) {
                if(!updatePositionsLocally) {
                    SetObservedPosition();
                }
                if(!updateRotationsLocally) {
                    SetObservedRotation();
                }
            }

            else {
                transform.position = LerpBufferedPositions(WCNetClient.SendingTick - 1, percentageThroughTick);
                // Never do rotations. These are always done by the client
                transform.localScale = LerpBufferedScales(WCNetClient.SendingTick - 1, percentageThroughTick);
            }
        }
        
        // way 1 of doing things
        private void SetObservedPosition() {
            if(updatePositionsLocally)
                return;
            
            if(!receivedPositions.TryGetByTimestamp(WCNetClient.ObservingTick - 1, out Vector3 currentPosition))
                return;

            // If a previous position exists, lerp between previous and current position
            if(receivedPositions.TryGetByTimestamp(WCNetClient.ObservingTick - 2, out Vector3 previousPosition)) {
                transform.position = Vector3.Lerp(previousPosition, currentPosition, WCNetClient.PercentageThroughTick);

            // Otherwise teleport
            } else {
                transform.position = currentPosition;
            }
        }

        private void SetObservedRotation() {
            if(updateRotationsLocally)
                return;
            
            if(!receivedRotations.TryGetByTimestamp(WCNetClient.ObservingTick - 1, out Quaternion currentRotation))
                return;

            // If a previous Rotation exists, lerp between previous and current Rotation
            if(receivedRotations.TryGetByTimestamp(WCNetClient.ObservingTick - 2, out Quaternion previousRotation)) {
                transform.rotation = Quaternion.Lerp(previousRotation, currentRotation, WCNetClient.PercentageThroughTick);

            // Otherwise teleport
            } else {
                transform.rotation = currentRotation;
            }
        }

        public void SetTransformForTick(int tick, WTransformSerializable serializedTransform) {
            if (serializedTransform.position != null) {
                positionsBuffer[tick] = serializedTransform.position.Value;
                receivedPositions.SetValueAndTimestamp(serializedTransform.position.Value, tick);
            }     

            if (serializedTransform.rotation != null) {
                rotationsBuffer[tick] = serializedTransform.rotation.Value;
                receivedRotations.SetValueAndTimestamp(serializedTransform.rotation.Value, tick);
            }         

            if (serializedTransform.scale != null) {
                scalesBuffer[tick] = serializedTransform.scale.Value;
                receivedScales.SetValueAndTimestamp(serializedTransform.scale.Value, tick);
            }
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