using UnityEngine;
using Networking.Shared;

namespace Networking.Client {
    public class WCEntity : WEntityBase {
        public Vector3 positionFrom;
        public Quaternion rotationFrom;
        public Vector3 scaleFrom;

        public WTransformSerializable transformTo;

        private void Update() {
            float percentageThroughCurrentFrame = WCommon.GetPercentageTimeThroughCurrentTick();
            
            if(transformTo.position.HasValue)
                transform.position = positionFrom + (transformTo.position.Value - positionFrom) * percentageThroughCurrentFrame;

            if(transformTo.rotation.HasValue)
                transform.rotation = Quaternion.Lerp(rotationFrom, transformTo.rotation.Value, percentageThroughCurrentFrame);

            if(transformTo.scale.HasValue)
                transform.localScale = positionFrom + (transformTo.scale.Value - scaleFrom) * percentageThroughCurrentFrame;
        }

        
        public void AdvanceTick() {
            if(transformTo.position.HasValue) {
                transform.position = transformTo.position.Value;
                transformTo.position = null;
                positionFrom = transform.position;
            }
                
            if(transformTo.rotation.HasValue) {
                transform.rotation = transformTo.rotation.Value;
                transformTo.rotation = null;
                rotationFrom = transform.rotation;
            }

            if(transformTo.scale.HasValue) {
                transform.localScale = transformTo.scale.Value;
                transformTo.scale = null;
                scaleFrom = transform.localScale;
            }
        }

        public void UpdateTransform(WTransformSerializable nextTransform) {
            transformTo = nextTransform;
        }


        public void Init(WSEntitySpawnPkt spawnPkt) {
            Id = spawnPkt.entity.entityId;

            transform.position = spawnPkt.entity.transform.position.GetValueOrDefault(Vector3.zero);
            transform.rotation = spawnPkt.entity.transform.rotation.GetValueOrDefault(Quaternion.identity);
            transform.localScale = spawnPkt.entity.transform.scale.GetValueOrDefault(Vector3.one);

            positionFrom = transform.position;
            rotationFrom = transform.rotation;
            scaleFrom = transform.localScale;
        }

        public override void Kill(WEntityKillReason reason) {
            Destroy(gameObject);
        }
    }
}