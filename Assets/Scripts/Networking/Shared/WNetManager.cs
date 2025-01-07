using LiteNetLib;
using UnityEngine;
using Scenes;
using UnityEngine.SceneManagement;

using Networking.Server;
using Networking.Client;
using Controllers.Shared;
using TMPro;
using System;
using UnityEditor.Networking.PlayerConnection;

namespace Networking.Shared {
    public class WNetManager : BaseSingleton<WNetManager> {
        public const string CONNECTION_KEY = "WW 0.01";

        [SerializeField] private GameObject serverPrefab;
        [SerializeField] private GameObject clientPrefab;
        [SerializeField] private Transform entitiesHolder;
        [Space(10)]
        [SerializeField] private TMP_InputField clientAddress;
        [SerializeField] private TMP_InputField clientPort;
        [Space(10)]
        [SerializeField] private TMP_InputField serverPort;
        private NetManager netManager;
        public WSNetServer WcNetServer { get; private set; }
        public WCNetClient WcNetClient { get; private set; }
        public static bool IsServer { get { return Instance.WcNetServer != null; } }
        public static bool IsClient { get { return Instance.WcNetClient != null; } }

        protected override void Awake() {
            base.Awake();

            DontDestroyOnLoad(gameObject);

            WPrefabLookup.Init();
            ControlsManager.Init();
        }

        void Update() => netManager?.PollEvents();

        public static void Disconnect(WDisconnectInfo info) {
            if(IsServer) {
                throw new Exception("Not yet implemented!");
            } else {
                Instance.StopClient();
            }
        }


        public void StartClient() {
            if(WcNetClient != null)
                return;
            WcNetClient = Instantiate(clientPrefab).GetComponent<WCNetClient>();

            netManager = new NetManager(WcNetClient) {
                AutoRecycle = true,
                IPv6Enabled = false
            };
            netManager.Start();
            netManager.Connect(clientAddress.text, ushort.Parse(clientPort.text), CONNECTION_KEY);

            SceneManager.LoadScene(sceneBuildIndex: (int)SceneType.Game);
        }


        private void StopClient() {
            Debug.Log("Stopping client!");
            netManager.Stop();
            netManager = null;  // This may or may not be necessary

            if(WcNetClient != null) {
                Debug.Log("Destroying it too!");
                Destroy(WcNetClient);
            }

            WcNetClient = null;

            SceneManager.LoadScene(sceneBuildIndex: (int)SceneType.MainMenu);
        }


        public void StartServer() {      
            SceneManager.LoadScene(sceneBuildIndex: (int)SceneType.Game);
            WSEntityManager.spawnHolder = entitiesHolder;
            GameObject serverObject = Instantiate(serverPrefab);
            WcNetServer = serverObject.GetComponent<WSNetServer>();
            WcNetServer.Init(ushort.Parse(serverPort.text));
        }
    }
}
