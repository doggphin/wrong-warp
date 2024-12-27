using LiteNetLib;
using UnityEngine;
using Scenes;
using UnityEngine.SceneManagement;

using Networking.Server;
using Networking.Client;
using Controllers.Shared;
using TMPro;

namespace Networking.Shared {
    public class WNetManager : MonoBehaviour {
        [SerializeField] private GameObject serverPrefab;
        public WSNetServer WNetServer { get; private set; }

        [SerializeField] private GameObject clientPrefab;
        public WCNetClient WNetClient { get; private set; }

        public static WNetManager Instance { get; private set; }

        public static bool IsServer { get { return Instance.WNetServer != null; } }
        public static bool IsClient { get { return Instance.WNetClient != null; } }

        private static WInputsSerializable[] inputPackets = new WInputsSerializable[WCommon.TICKS_PER_SNAPSHOT];

        [SerializeField] private GameObject entitiesHolder;

        [Space(10)]
        [SerializeField] private TMP_InputField clientAddress;
        [SerializeField] private TMP_InputField clientPort;
        [Space(10)]
        [SerializeField] private TMP_InputField serverPort;


        private void Awake() {
            if (Instance != null)
                Destroy(gameObject);

            Instance = this;
            DontDestroyOnLoad(gameObject);

            WPrefabLookup.Init();
            ControlsManager.Init();

            for(int i=0; i<WCommon.TICKS_PER_SNAPSHOT; i++) {
                inputPackets[i] = new();
            }
        }

        private void OnDisconnect(DisconnectInfo info) {
            Debug.Log($"Disconnected: {info.Reason}");

            Destroy(WNetClient.gameObject);
            WNetClient = null;

            SceneManager.LoadScene(sceneBuildIndex: (int)SceneType.MainMenu);
        }


        public void StartClient() {
            WCEntityManager.SpawnHolder = entitiesHolder;

            GameObject clientObject = Instantiate(clientPrefab);
            WNetClient = clientObject.GetComponent<WCNetClient>();

            WNetClient.Init();
            Debug.Log($"{clientAddress.text}:{clientPort.text}");
            WNetClient.Connect(clientAddress.text, ushort.Parse(clientPort.text), OnDisconnect);

            SceneManager.LoadScene(sceneBuildIndex: (int)SceneType.Game);
        }


        public void StartServer() {      
            SceneManager.LoadScene(sceneBuildIndex: (int)SceneType.Game);

            GameObject serverObject = Instantiate(serverPrefab);
            WNetServer = serverObject.GetComponent<WSNetServer>();
            WSEntityManager.SpawnHolder = entitiesHolder;
            WNetServer.Init(ushort.Parse(serverPort.text));
        }
    }
}
