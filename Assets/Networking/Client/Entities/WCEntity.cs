using UnityEngine;
using Networking.Shared;

namespace Networking.Client {
    public class WCEntity : WEntityBase {
        public void Init(WSEntitySpawnPkt spawnPkt) {
            Id = spawnPkt.entity.entityId;

            transform.position = spawnPkt.entity.transform.position.GetValueOrDefault(Vector3.zero);
            transform.rotation = spawnPkt.entity.transform.rotation.GetValueOrDefault(Quaternion.identity);
            transform.localScale = spawnPkt.entity.transform.scale.GetValueOrDefault(Vector3.one);

            // WEntitySpawnReason reason = spawnPkt.reason;
        }

        public override void Kill(WEntityKillReason reason) {
            Destroy(gameObject);
        }
    }
}