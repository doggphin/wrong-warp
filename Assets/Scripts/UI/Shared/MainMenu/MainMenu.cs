using TMPro;
using UnityEngine;
using Networking.Shared;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private TMP_InputField clientAddress;
    [SerializeField] private TMP_InputField clientPort;
    [Space(10)]
    [SerializeField] private TMP_InputField serverPort;
    [Space(10)]
    [SerializeField] private GameObject statusInfoContainer;
    [SerializeField] private TMP_Text statusInfoText;

    void Awake() {
        WWNetManager.Disconnected += SetStatusDisconnected;
    }

    void OnDestroy() {
        WWNetManager.Disconnected -= SetStatusDisconnected;
    }

    private void SetStatusDisconnected(WDisconnectInfo info) {
        statusInfoContainer.SetActive(true);
        statusInfoText.color = info.wasExpected ? Color.green : Color.red;
        statusInfoText.text = info.reason;
    }

    public void StartServer() {
        ushort port = ushort.Parse(serverPort.text);
        WWNetManager.Instance.StartServer(port);
    }

    public void StartClient() {
        string address = clientAddress.text;
        ushort port = ushort.Parse(clientPort.text);
        WWNetManager.Instance.StartClient(address, port);
    }
}
