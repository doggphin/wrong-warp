using UnityEngine;
using Networking.Shared;

namespace Networking.Client {
    public class WCEntity : WEntityBase {
        public CircularTickBuffer<int> lastReceivedPositionUpdateTicks = new();
        public CircularTickBuffer<int> lastReceivedRotationUpdateTicks = new();
        public CircularTickBuffer<int> lastReceivedScaleUpdateTicks = new();

        public bool isMyPlayer;

        private void Update() {
            float percentageThroughTick = WCNetClient.PercentageThroughTick;

            if(!isMyPlayer) {
                // If updatePositionsLocally, don't update it automatically
                if(!updatePositionsLocally) {
                    // If received an update on this tick,
                    if(lastReceivedPositionUpdateTicks[WCNetClient.ObservingTick] == WCNetClient.ObservingTick) {
                        // If the last received position exists, lerp between that-
                        // Otherwise, teleport to the new position, since we have no idea how far to go. There's probably better ways to do this
                        transform.position = (lastReceivedPositionUpdateTicks[WCNetClient.ObservingTick - 1] == WCNetClient.ObservingTick - 1) ?
                            LerpBufferedPositions(WCNetClient.ObservingTick, percentageThroughTick) :
                            positionsBuffer[WCNetClient.ObservingTick];
                    }
                }
                if(!updateRotationsLocally)
                    transform.rotation = LerpBufferedRotations(WCNetClient.ObservingTick, percentageThroughTick);
                if(!updateScalesLocally)
                    transform.localScale = LerpBufferedScales(WCNetClient.ObservingTick, percentageThroughTick);
            }
            
            else {
                transform.position = LerpBufferedPositions(WCNetClient.SendingTick, percentageThroughTick);
                // Never do rotations. These are always done by the client
                transform.localScale = LerpBufferedScales(WCNetClient.SendingTick, percentageThroughTick);
            }
        }

        public void SetTransformForTick(int tick, WTransformSerializable transform) {
            if (transform.position != null) {
                positionsBuffer[tick] = transform.position.Value;
                lastReceivedPositionUpdateTicks[tick] = tick;
            }     

            if (transform.rotation != null) {
                rotationsBuffer[tick] = transform.rotation.Value;
                lastReceivedRotationUpdateTicks[tick] = tick;
            }         

            if (transform.scale != null) {
                scalesBuffer[tick] = transform.scale.Value;
                lastReceivedScaleUpdateTicks[tick] = tick;
            }
        }


        public void Init(WSEntitySpawnPkt spawnPkt) {
            Id = spawnPkt.entity.entityId;

            positionsBuffer[WCNetClient.ObservingTick] = spawnPkt.entity.transform.position.GetValueOrDefault(Vector3.zero);
            rotationsBuffer[WCNetClient.ObservingTick] = spawnPkt.entity.transform.rotation.GetValueOrDefault(Quaternion.identity);
            scalesBuffer[WCNetClient.ObservingTick] = spawnPkt.entity.transform.scale.GetValueOrDefault(Vector3.one);

            positionsBuffer[WCNetClient.ObservingTick - 1] = positionsBuffer[WCNetClient.ObservingTick];
            rotationsBuffer[WCNetClient.ObservingTick - 1] = rotationsBuffer[WCNetClient.ObservingTick];
            scalesBuffer[WCNetClient.ObservingTick - 1] = scalesBuffer[WCNetClient.ObservingTick];
        }

        public override void Kill(WEntityKillReason reason) {
            Debug.Log("RIP!!!!!");
            Destroy(gameObject);
        }
    }
}