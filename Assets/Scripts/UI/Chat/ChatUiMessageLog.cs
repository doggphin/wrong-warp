using System.Collections.Generic;
using System.Linq;
using Networking.Shared;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class ChatUiMessageLog : MonoBehaviour {
    private const int BACKLOG_CAPACITY = 10;

    [SerializeField] GameObject messagePrefab;

    private ChatUiMessage[] messageLog = new ChatUiMessage[BACKLOG_CAPACITY];
    
    void Awake() {
        for(int i=0; i<BACKLOG_CAPACITY; i++) {
            GameObject instantiatedMessage = Instantiate(messagePrefab);
            messageLog[i] = instantiatedMessage.GetComponent<ChatUiMessage>();
        }
    }

    public void AddMessage(WSChatMessagePkt newMessage) {
        /*if(messageLog.Length > BACKLOG_CAPACITY)
            messageLog.RemoveAt(0);

        messageLog.Append(newMessage);
        UpdateMessages();*/
    }

    public void ClearMessages() {
        //messageLog.Clear();
    }

    private void UpdateMessages() {
        string logText = "";

    }
}