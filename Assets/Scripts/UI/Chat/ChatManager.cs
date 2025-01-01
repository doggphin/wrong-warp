using System;
using Controllers.Shared;
using Networking.Shared;
using Unity.VisualScripting;
using UnityEngine;

public class ChatManager : BaseUiElement
{
    public const float MAX_LOCAL_CHAT_DISTANCE = 32;

    [SerializeField] private ChatUiMessageLog messageLog;
    [SerializeField] private ChatUiMessageInput messageInput;

    public static ChatManager Instance { get; private set; }

    public static event Action<string> ChatMessageSent;
    
    void Start() {
        if(Instance) {
            Destroy(gameObject);
        }
        Instance = this;

        DisableAndListen();
    }


    public override void Open()
    {
        if(IsOpen)
            return;

        IsOpen = true;
        
        EnableAndStopListening();
    }
    private void EnableAndStopListening() {
        messageInput.gameObject.SetActive(true);
        messageInput.StartTyping();
        ControlsManager.TypingClicked -= () => UiManager.SetActiveUiElement(this, true);
    }
    

    public override void Close()
    {
        if(!IsOpen)
            return;

        IsOpen = false;
        
        DisableAndListen();
    }
    private void DisableAndListen() {
        messageInput.gameObject.SetActive(false);
        messageInput.StopTyping();
        ControlsManager.TypingClicked += () => UiManager.SetActiveUiElement(this, true);
    }


    public static void ReceiveChatMessage(WSChatMessagePkt chatMessageInfo) {

    }


    public static void SendChatMessage() {
        string message = Instance.messageInput.GetInput();
        ChatMessageSent.Invoke(message);
        Instance.messageInput.ClearInput();
    }
}
