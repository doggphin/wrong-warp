using Networking;
using System.Collections.Generic;
using UnityEngine;

namespace Networking
{
    public class NetManager : MonoBehaviour
    {
        [System.Serializable]
        public struct InspectorNetPrefabIdToPrefab
        {
            public NetPrefabId prefabId;
            public GameObject prefab;
        }

        [SerializeField] private InspectorNetPrefabIdToPrefab[] netPrefabIdArr;
        public readonly Dictionary<NetPrefabId, GameObject> netPrefabIdDict = new();

        public static NetManager instance;
        public readonly Dictionary<ulong, NetObject> activeNetObjects = new();
        public bool IsServer { get; private set; }
        public bool IsClient { get; private set; }
        public NetObject player;

        private void Awake()
        {
            if(instance)
            {
                Destroy(gameObject);
            }
            instance = this;
            DontDestroyOnLoad(gameObject);

            // Load netPrefabIdArr into a dictionary
            foreach(var entry in netPrefabIdArr)
            {
                if(entry.prefabId != NetPrefabId.Empty && entry.prefab != null)
                {
                    netPrefabIdDict.Add(entry.prefabId, entry.prefab);
                } else
                {
                    Debug.LogError("Empty prefab listed in NetManager NetPrefab ID list!");
                }
            }
        }


        public void RunAsClient(ushort serverPort)
        {

        }


        public void RunAsHost(ushort hostPort)
        {

        }
    }
}
