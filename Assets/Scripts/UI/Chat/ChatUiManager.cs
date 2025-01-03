using System;
using Controllers.Shared;
using Networking.Shared;
using Unity.VisualScripting;
using UnityEngine;

public class ChatUiManager : BaseUiElement
{
    public const float MAX_LOCAL_CHAT_DISTANCE = 32;

    [SerializeField] private ChatUiMessageLog messageLog;
    [SerializeField] private ChatUiMessageInput messageInput;

    public static ChatUiManager Instance { get; private set; }

    public static Action<string> SendChatMessage;
    
    void Start() {
        if(Instance) {
            Destroy(gameObject);
        }
        Instance = this;

        DisableMessageInput();
    }


    public override void Open()
    {
        if(IsOpen)
            return;

        IsOpen = true;
        
        EnableMessageInput();
    }


    private void EnableMessageInput() {
        messageInput.gameObject.SetActive(true);
        messageInput.StartTyping();

        ControlsManager.ConfirmClicked += () => ConfirmChatMessage();
        ControlsManager.ChatClicked -= () => UiManager.SetActiveUiElement(this, true);
    }
    

    public override void Close()
    {
        if(!IsOpen)
            return;

        IsOpen = false;
        
        DisableMessageInput();
    }
    private void DisableMessageInput() {
        messageInput.gameObject.SetActive(false);
        Instance.messageInput.ClearInput();
        messageInput.StopTyping();

        ControlsManager.ConfirmClicked -= () => ConfirmChatMessage();
        ControlsManager.ChatClicked += () => UiManager.SetActiveUiElement(this, true);
    }


    public static void ReceiveChatMessage(WSChatMessagePkt chatMessageInfo) {
        Instance.messageLog.AddMessage(chatMessageInfo);
    }


    public static void ConfirmChatMessage() {
        Debug.Log("Sending!");

        string message = Instance.messageInput.GetInput();
        SendChatMessage?.Invoke(message);
        
        UiManager.CloseActiveUiElement();
    }
}
