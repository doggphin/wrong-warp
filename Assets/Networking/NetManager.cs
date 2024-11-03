using Networking;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;
using UnityEngine;

namespace Networking
{
    public enum SessionMode
    {
        Offline,
        Server,
        Client
    }


    public class NetClient
    {
        public NetObject playerObject { get; private set; }
        public ulong playerId { get; private set; }

        public NetClient(NetObject playerObject, ulong playerId)
        {
            this.playerObject = playerObject;
            this.playerId = playerId;
        }
    }


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

        public static NetManager Instance { get; private set; }
        public readonly Dictionary<ulong, NetObject> activeNetObjects = new();
        public bool IsServer { get; private set; }
        public bool IsClient { get; private set; }
        public NetObject player;


        private void Awake()
        {
            if(Instance)
            {
                Destroy(gameObject);
            }
            Instance = this;
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

        SessionMode sessionMode = SessionMode.Offline;
        NetUdpClient netUdpClient;
        NetUdpServer netUdpServer;
        public void StartAsClient(ushort port = 2791)
        {
            if(netUdpClient != null)
            {
                netUdpClient.Disconnect();
            }
            netUdpClient = new NetUdpClient(port);
        }

        public void StartAsServer(ushort port = 2791)
        {
            netUdpServer = new NetUdpServer(port);
        }
    }
}
