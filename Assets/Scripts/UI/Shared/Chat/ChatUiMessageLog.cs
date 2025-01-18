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
    private int messageLogIndex = 0;
    
    void Awake() {
        for(int i=0; i<BACKLOG_CAPACITY; i++) {
            messageLog[i] = Instantiate(messagePrefab, transform).GetComponent<ChatUiMessage>();
        }
    }

    public void AddMessage(WSChatMessagePkt newMessage) {
        messageLogIndex = (messageLogIndex + 1) % BACKLOG_CAPACITY;

        ChatUiMessage uiMessage = messageLog[messageLogIndex];
        
        uiMessage.gameObject.transform.SetAsFirstSibling();
        uiMessage.SetNewMessage(newMessage);
    }
}