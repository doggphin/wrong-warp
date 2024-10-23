using UnityEngine;

namespace Networking
{
    struct MsgServer_NetObjectUpdate
    {
        public Vector3? position;
        public Quaternion? rotation;
        public Vector3? scale;
    }
}
