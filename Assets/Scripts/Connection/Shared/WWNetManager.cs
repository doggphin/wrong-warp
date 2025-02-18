using LiteNetLib;
using UnityEngine;

using Networking.Server;
using Networking.Client;
using Controllers.Shared;

using System.Collections.Generic;
using System;
using Audio.Shared;
using Inventories;

namespace Networking.Shared {
    [RequireComponent(typeof(AsyncAudioLookup))]
    [RequireComponent(typeof(EntityPrefabLookup))]
    [RequireComponent(typeof(ItemLookup))]
    [RequireComponent(typeof(InteractableIconLookup))]
    public class WWNetManager : BaseSingleton<WWNetManager> {
        [SerializeField] private GameObject mainMenuPrefab;
        [SerializeField] private GameObject serverPrefab;
        [SerializeField] private GameObject clientPrefab;
        [SerializeField] private GameObject uiManagerPrefab;

        public static NetManager BaseNetManager { get; private set; }
        public static List<NetPeer> ConnectedPeers => BaseNetManager.ConnectedPeerList;
        public SNetManager WsNetServer { get; private set; }
        public CNetManager WcNetClient { get; private set; }
        public static bool IsServer { get { return Instance.WsNetServer != null; } }
        public static bool IsClient { get { return Instance.WcNetClient != null; } }

        private ITicker ticker;
        public static int GetTick() => Instance.ticker.GetTick();
        public static float GetPercentageThroughTick() => Instance.ticker.GetPercentageThroughTick();
        public static float GetPercentageThroughTickCurrentFrame() => Instance.ticker.GetPercentageThroughTickCurrentFrame();
        
        public static Action<WDisconnectInfo> Disconnected;

        private MainMenu mainMenu;
        private UiManager uiManager;

        private void EnterMainMenu() {
            if(mainMenu == null)
                mainMenu = Instantiate(mainMenuPrefab).GetComponent<MainMenu>();
            if(uiManager != null)
                Destroy(uiManager.gameObject);
            Cursor.lockState = CursorLockMode.None;
        }

        private void StopMainMenu() {
            if(mainMenu != null)
                Destroy(mainMenu.gameObject);
            if(uiManager == null)
                uiManager = Instantiate(uiManagerPrefab).GetComponent<UiManager>();
        }

        protected override void Awake() {
            base.Awake();

            Physics.simulationMode = SimulationMode.Script;
            
            EnterMainMenu();
        }

        void Update() => BaseNetManager?.PollEvents();

        
        public void StartClient(string address, ushort port) {
            print("Trying to start client!");
            if(WcNetClient != null)
                return;

            WcNetClient = Instantiate(clientPrefab).GetComponent<CNetManager>();
            ticker = WcNetClient;

            BaseNetManager = SetNewNetManager(WcNetClient);
            BaseNetManager.Start();
            BaseNetManager.Connect(address, port, NetCommon.CONNECTION_KEY);

            StopMainMenu();
        }
        
        

        public void StartServer(ushort port) {
            print("Trying to start server!");
            if(WsNetServer != null)
                return;

            WsNetServer = Instantiate(serverPrefab).GetComponent<SNetManager>();
            ticker = WsNetServer;

            BaseNetManager = SetNewNetManager(WsNetServer);
            BaseNetManager.Start(port);
            WsNetServer.Activate();

            StopMainMenu();
        }


        public static void Disconnect(WDisconnectInfo info) {
            BaseNetManager.Stop();
            BaseNetManager = null;

            Instance.ticker = null;

            // Not great to do this here, but it's common between both client/server OnDestroy()s
            ControlsManager.SetPlayer(null);
            ControlsManager.DeactivateControls();

            if(Instance.WcNetClient != null)
                Destroy(Instance.WcNetClient);
            if(Instance.WsNetServer != null)
                Destroy(Instance.WsNetServer);

            Instance.WcNetClient = null;
            Instance.WsNetServer = null;
            Instance.ticker = null;

            Disconnected?.Invoke(info);

            Instance.EnterMainMenu();
        }


        private NetManager SetNewNetManager(INetEventListener netEventListener) {
            return new NetManager(netEventListener) {
                AutoRecycle = true,
                IPv6Enabled = false,
                DisconnectTimeout = NetCommon.TIMEOUT_MS,
                ReconnectDelay = 500,
                MaxConnectAttempts = 5,
                MtuOverride = 65535,
            };
        }
    }
}
