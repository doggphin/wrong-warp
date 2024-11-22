
using UnityEngine;

namespace Networking.Shared {
    public abstract class WEntityBase : MonoBehaviour {
        public int Id { get; protected set; }
        public WPrefabId PrefabId { get; protected set; }
        protected bool isDead;
        
        public abstract void Kill(WEntityKillReason reason);
    }
}