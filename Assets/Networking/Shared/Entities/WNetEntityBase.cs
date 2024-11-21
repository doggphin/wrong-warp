
using UnityEngine;

namespace Networking.Shared {
    public abstract class WNetEntityBase : MonoBehaviour {
        public int Id { get; protected set; }
        protected bool isDead;
        
        public abstract void Kill(WEntityKillReason reason);
    }
}