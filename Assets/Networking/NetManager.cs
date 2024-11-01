using Networking;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;
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


        UdpClient udpClient;
        public void StartAsClient(ushort serverPort = 2791)
        {
            udpClient = new UdpClient(serverPort);  
        }
        private async void ReceiveMessage(ushort serverPort)
        {
            udpClient = new UdpClient(serverPort);

            byte[] data = new byte[NetConsts.MAX_NETMESSAGE_PACKET_SIZE];
            while(true)
            {
                UdpReceiveResult recv = await udpClient.ReceiveAsync();
                data = recv.Buffer;
            }
        }

        public void StartAsHost(ushort hostPort = 2791)
        {

        }

        /* tick = ulong
         * following data type 
         */
        public void Client_ProcessPacket(byte[] data)
        {

        }
    }
}
