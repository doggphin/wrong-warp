using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

namespace Networking
{
    public class WNetObject : MonoBehaviour
    {
        public int id;
        public WNetPrefabId prefabId;

        public bool srv_updatePos;
        public bool srv_updateRot;
        public bool srv_updateScale;

        private Vector3 srv_previousPos;
        private Quaternion srv_previousRot;
        private Vector3 srv_previousScale;


        public void Init(WNetPrefabId prefabId) {
            this.prefabId = prefabId;
        }
    }
}
