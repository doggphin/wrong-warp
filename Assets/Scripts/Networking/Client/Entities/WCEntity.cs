using UnityEngine;
using Networking.Shared;

namespace Networking.Client {
    public class WCEntity : WEntityBase {
        private void Update() {
            float percentageThroughCurrentFrame = WCNetClient.PercentageThroughTick;
            
            if(HasMoved && !renderPersonalPositionUpdates)
                transform.position = previousPosition + (currentPosition - previousPosition) * percentageThroughCurrentFrame;

            if(HasRotated && !renderPersonalRotationUpdates)
                transform.rotation = Quaternion.Lerp(previousRotation, currentRotation, percentageThroughCurrentFrame);
            
            if(HasScaled && !renderPersonalScaleUpdates)
                transform.localScale = previousScale + (currentScale - previousScale) * percentageThroughCurrentFrame;
        }
        

        public void ApplyPreviousTransform() {
            if(HasMoved && !renderPersonalPositionUpdates)
                previousPosition = currentPosition;
            
            if(HasRotated && !renderPersonalRotationUpdates)
                previousRotation = currentRotation;

            if(HasScaled && !renderPersonalScaleUpdates)
                previousScale = currentScale;
        }


        public void UpdateTransform(WTransformSerializable nextTransform) {
            if (nextTransform.position != null)
                currentPosition = nextTransform.position.Value;

            if (nextTransform.rotation != null)
                currentRotation = nextTransform.rotation.Value;

            if (nextTransform.scale != null)
                currentScale = nextTransform.scale.Value; 
        }


        public void Init(WSEntitySpawnPkt spawnPkt) {
            Id = spawnPkt.entity.entityId;

            currentPosition = spawnPkt.entity.transform.position.GetValueOrDefault(Vector3.zero);
            currentRotation = spawnPkt.entity.transform.rotation.GetValueOrDefault(Quaternion.identity);
            currentScale = spawnPkt.entity.transform.scale.GetValueOrDefault(Vector3.one);

            previousPosition = transform.position;
            previousRotation = transform.rotation;
            previousScale = transform.localScale;
        }

        public override void Kill(WEntityKillReason reason) {
            Debug.Log("RIP!!!!!");
            Destroy(gameObject);
        }
    }
}