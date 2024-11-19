using LiteNetLib;
using UnityEngine;
using Scenes;
using UnityEngine.SceneManagement;

using Networking.Server;
using Networking.Client;

namespace Networking.Shared {
    public class WNetManager : MonoBehaviour {
        [SerializeField] private GameObject serverPrefab;
        public WNetServer WNetServer { get; private set; }

        [SerializeField] private GameObject clientPrefab;
        public WNetClient WNetClient { get; private set; }

        public static WNetManager Instance { get; private set; }

        public static bool IsServer { get { return Instance.WNetServer != null; } }
        public static bool IsClient { get { return Instance.WNetClient != null; } }


        private void Awake() {
            if (Instance != null)
                Destroy(gameObject);

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDisconnect(DisconnectInfo info) {
            Debug.Log($"Disconnected: {info.Reason}");

            Destroy(WNetClient.gameObject);
            WNetClient = null;

            SceneManager.LoadScene(sceneBuildIndex: (int)SceneType.MainMenu);
        }


        public void StartClient(string address) {
            GameObject clientObject = Instantiate(clientPrefab);
            WNetClient = clientObject.GetComponent<WNetClient>();
            WNetClient.Connect("localhost", OnDisconnect);

            SceneManager.LoadScene(sceneBuildIndex: (int)SceneType.Game);
        }


        public void StartServer() {
            Debug.Log("Test");

            GameObject serverObject = Instantiate(serverPrefab);
            WNetServer = serverObject.GetComponent<WNetServer>();
            WNetServer.StartServer();

            SceneManager.LoadScene(sceneBuildIndex: (int)SceneType.Game);

            WNetEntityManager.Instance.SpawnEntity(WNetPrefabId.Test, true);
        }
    }
}
