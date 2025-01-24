using System;
using Controllers.Shared;
using Networking.Shared;
using UnityEngine;

public class ChatUiManager : BaseUiElement<ChatUiManager>
{
    public const float MAX_LOCAL_CHAT_DISTANCE = 32;

    [SerializeField] private ChatUiMessageLog messageLog;
    [SerializeField] private ChatUiMessageInput messageInput;

    public static Action<string> SendChatMessage;
    
    protected override void Awake() {
        SPacket<SChatMessagePkt>.ApplyUnticked += ReceiveChatMessage;
        base.Awake();
    }

    protected override void OnDestroy() {
        SPacket<SChatMessagePkt>.ApplyUnticked -= ReceiveChatMessage;
        base.OnDestroy();
    }


    void Start() {
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

        ControlsManager.ConfirmClicked += ConfirmChatMessage;
        ControlsManager.ChatClicked -= SetAsActiveUi;
    }

    public static void ConfirmChatMessage() {
        string message = Instance.messageInput.GetInput();
        SendChatMessage?.Invoke(message);
        
        UiManager.CloseActiveUiElement();
    }

    private void SetAsActiveUi() {
        UiManager.SetActiveUiElement(this, true);
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
        messageInput.ClearInput();
        messageInput.StopTyping();

        ControlsManager.ConfirmClicked -= ConfirmChatMessage;
        ControlsManager.ChatClicked += SetAsActiveUi;
    }


    public static void ReceiveChatMessage(SChatMessagePkt chatMessageInfo) {
        Instance.messageLog.AddMessage(chatMessageInfo);
    }
}
