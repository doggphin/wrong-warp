using LiteNetLib;
using UnityEngine;
using Scenes;
using UnityEngine.SceneManagement;

using Networking.Server;
using Networking.Client;
using Controllers.Shared;
using Audio.Shared;

using System.Collections.Generic;
using System;
using System.Collections;
using Inventories;

namespace Networking.Shared {
    public class WNetManager : BaseSingleton<WNetManager> {
        [SerializeField] private GameObject serverPrefab;
        [SerializeField] private GameObject clientPrefab;

        public static NetManager BaseNetManager { get; private set; }
        public static List<NetPeer> ConnectedPeers => BaseNetManager.ConnectedPeerList;
        public WSNetServer WsNetServer { get; private set; }
        public WCNetClient WcNetClient { get; private set; }
        public static bool IsServer { get { return Instance.WsNetServer != null; } }
        public static bool IsClient { get { return Instance.WcNetClient != null; } }

        private ITicker ticker;
        public static int GetTick() => Instance.ticker.GetTick();
        public static float GetPercentageThroughTick() => Instance.ticker.GetPercentageThroughTick();
        public static float GetPercentageThroughTickCurrentFrame() => Instance.ticker.GetPercentageThroughTickCurrentFrame();
        
        public static Action<WDisconnectInfo> Disconnected;

        protected override void Awake() {
            base.Awake();

            DontDestroyOnLoad(gameObject);

            ControlsManager.Init();
        }

        void Update() => BaseNetManager?.PollEvents();
 

        
        public void StartClient(string address, ushort port) {
            print("Trying to start client!");
            if(WcNetClient != null)
                return;

            WcNetClient = Instantiate(clientPrefab).GetComponent<WCNetClient>();

            BaseNetManager = SetNewNetManager(WcNetClient);
            BaseNetManager.Start();
            BaseNetManager.Connect(address, port, WCommon.CONNECTION_KEY);

            ticker = WcNetClient;

            SceneManager.LoadScene(sceneBuildIndex: (int)SceneType.Game);
        }
        
        

        public void StartServer(ushort port) {
            print("Trying to start server!");
            if(WsNetServer != null)
                return;

            WsNetServer = Instantiate(serverPrefab).GetComponent<WSNetServer>();

            BaseNetManager = SetNewNetManager(WsNetServer);
            BaseNetManager.Start(port);
            WsNetServer.Activate();

            ticker = WsNetServer;

            SceneManager.LoadScene(sceneBuildIndex: (int)SceneType.Game);
        }


        public static void Disconnect(WDisconnectInfo info) {
            BaseNetManager.Stop();
            BaseNetManager = null;

            // Not great to do this here, but it's common between both client/server OnDestroy()s
            ControlsManager.player = null;
            ControlsManager.Deactivate();
            UiManager.CloseActiveUiElement();

            if(Instance.WcNetClient != null)
                Destroy(Instance.WcNetClient);
            if(Instance.WsNetServer != null)
                Destroy(Instance.WsNetServer);

            Instance.WcNetClient = null;
            Instance.WsNetServer = null;
            Instance.ticker = null;

            Instance.StartCoroutine(LoadMenuFromDisconnect(info));
        }

        private static IEnumerator LoadMenuFromDisconnect(WDisconnectInfo info) {
            var asyncLoadScene = SceneManager.LoadSceneAsync(sceneBuildIndex: (int)SceneType.MainMenu);

            while (!asyncLoadScene.isDone){
                yield return null;
            }

            Cursor.lockState = CursorLockMode.None;
            Disconnected?.Invoke(info);
        }


        private NetManager SetNewNetManager(INetEventListener netEventListener) {
            return new NetManager(netEventListener) {
                AutoRecycle = true,
                IPv6Enabled = false,
                DisconnectTimeout = WCommon.TIMEOUT_MS,
                ReconnectDelay = 500,
                MaxConnectAttempts = 5
            };
        }
    }
}
