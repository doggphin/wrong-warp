using LiteNetLib;
using UnityEngine;
using Scenes;
using UnityEngine.SceneManagement;

using Code.Server;
using Code.Client;
using System;

namespace Code.Shared {
    public class WNetManager : MonoBehaviour {
        [SerializeField] private GameObject serverPrefab;
        public WNetServer WNetServer { get; private set; }

        [SerializeField] private GameObject clientPrefab;
        public WNetClient WNetClient { get; private set; }


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
        }
    }
}
