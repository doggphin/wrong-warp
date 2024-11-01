using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

namespace Networking
{
    public class NetObject : MonoBehaviour
    {
        public ulong netId = 0;

        public NetPrefabId prefabId = NetPrefabId.Empty;

        public bool srv_updatePos;
        public bool srv_updateRot;
        public bool srv_updateScale;

        private Vector3 srv_previousPos;
        private Quaternion srv_previousRot;
        private Vector3 srv_previousScale;

        private List<INetMessage> srv_events = new();

        public void SrvPushEvent(INetMessage srvEvent) {
            srv_events.Add(srvEvent);
        }
        public void SrvPopEvents(out List<INetMessage> outList)
        {
            //srv_events.Prepend()

            outList = srv_events;
            srv_events = new();
        }

        public virtual void Init(ulong netId, NetPrefabId prefabId, Vector3 position, Quaternion rotation, Vector3 scale) { }
    }
}
