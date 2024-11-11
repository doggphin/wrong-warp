using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Scenes;

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
        public Dictionary<NetPrefabId, GameObject> netPrefabIdDict;
        public Dictionary<long, NetObject> objects = new();

        public long Tick { get; private set; }
        public bool IsServer { get; private set; }
        public bool IsClient { get; private set; }
        public NetObject player;

        public NetClient NetClient { get; private set; }
        public NetServer NetServer { get; private set; }

        public static NetManager Instance { get; private set; }
        private void Awake()
        {
            if(Instance)
            {
                Destroy(gameObject);
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Load netPrefabIdArr into a dictionary
            Instance.netPrefabIdDict = new();
            foreach (var entry in netPrefabIdArr)
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


        public void StartAsClient(ushort port)
        {
            NetClient = new NetClient("127.0.0.1", port);
            SceneManager.LoadScene((int)SceneType.Game);
        }
        public void StartAsClient() {
            StartAsClient(8000);
        }

        public void StartAsServer(ushort port)
        {
            NetServer = new NetServer("127.0.0.1", port);
            SceneManager.LoadScene((int)SceneType.Game);
        }
        public void StartAsServer() {
            StartAsServer(8000);
        }
    }
}
