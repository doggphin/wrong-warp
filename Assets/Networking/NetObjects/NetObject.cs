using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

namespace Networking
{
    public class NetObject : MonoBehaviour
    {
        public long id;
        private List<ServerNetMessage> srv_events;
        public NetPrefabId prefabId;

        public bool srv_updatePos;
        public bool srv_updateRot;
        public bool srv_updateScale;

        private Vector3 srv_previousPos;
        private Quaternion srv_previousRot;
        private Vector3 srv_previousScale;


        public void Init(NetPrefabId prefabId) {
            this.prefabId = prefabId;

            srv_events = new();
        }

        public void SrvPushEvent(ServerNetMessage srv_netMessage) {
            srv_events.Add(srv_netMessage);
        }

        public void SrvPopEvents(List<ServerNetMessage> srv_allMessages) {
            srv_allMessages.AddRange(srv_events);
            srv_events = new();
        }
    }
}
