using Unity.VisualScripting;
using UnityEngine;

namespace Networking
{
    public class NetObject : MonoBehaviour
    {
        public ulong netId = 0;

        public NetPrefabId prefabId = NetPrefabId.Empty;

        public bool updatePos;
        public bool updateRot;
        public bool updateScale;
    }
}
