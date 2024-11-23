using LiteNetLib;
using UnityEngine;
using Scenes;
using UnityEngine.SceneManagement;

using Networking.Server;
using Networking.Client;
using Unity.VisualScripting;

namespace Networking.Shared {
    public class WNetManager : MonoBehaviour {
        [SerializeField] private GameObject serverPrefab;
        public WSNetServer WNetServer { get; private set; }

        [SerializeField] private GameObject clientPrefab;
        public WCNetClient WNetClient { get; private set; }

        public static WNetManager Instance { get; private set; }

        public static bool IsServer { get { return Instance.WNetServer != null; } }
        public static bool IsClient { get { return Instance.WNetClient != null; } }

        [SerializeField] private GameObject entitiesHolder;


        private void Awake() {
            if (Instance != null)
                Destroy(gameObject);

            Instance = this;
            DontDestroyOnLoad(gameObject);

            WPrefabLookup.Init();
        }

        private void OnDisconnect(DisconnectInfo info) {
            Debug.Log($"Disconnected: {info.Reason}");

            Destroy(WNetClient.gameObject);
            WNetClient = null;

            SceneManager.LoadScene(sceneBuildIndex: (int)SceneType.MainMenu);
        }


        public void StartClient(string address) {
            WCEntityManager.SpawnHolder = entitiesHolder;
            GameObject clientObject = Instantiate(clientPrefab);
            WNetClient = clientObject.GetComponent<WCNetClient>();
            WNetClient.Connect("localhost", OnDisconnect);

            SceneManager.LoadScene(sceneBuildIndex: (int)SceneType.Game);
        }


        public void StartServer() {      
            WSEntityManager.SpawnHolder = entitiesHolder;
            GameObject serverObject = Instantiate(serverPrefab);
            WNetServer = serverObject.GetComponent<WSNetServer>();
            WNetServer.StartServer();

            SceneManager.LoadScene(sceneBuildIndex: (int)SceneType.Game);

            WSEntity entity = WSEntityManager.SpawnEntity(WPrefabId.Test, true, false, false, true);
            entity.gameObject.AddComponent<SpinnerTest>();
        }
    }
}
