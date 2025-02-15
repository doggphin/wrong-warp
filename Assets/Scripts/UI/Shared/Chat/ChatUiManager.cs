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

    public override bool RequiresMouse => false;
    public override bool AllowsMovement => false;

    protected override void Awake() {
        SPacket<SChatMessagePkt>.ApplyUnticked += ReceiveChatMessage;
        base.Awake();
        gameObject.SetActive(true);
        DisableMessageInput();
    }

    protected override void OnDestroy() {
        SPacket<SChatMessagePkt>.ApplyUnticked -= ReceiveChatMessage;
        base.OnDestroy();
    }


    public override void Open() {
        IsOpen = true;
        EnableMessageInput();
    }


    public override void Close() {
        IsOpen = false;
        DisableMessageInput();
    }


    private void EnableMessageInput() {
        messageInput.gameObject.SetActive(true);
        messageInput.StartTyping();

        ControlsManager.ConfirmClicked += ConfirmChatMessage;
    }

        private void DisableMessageInput() {
        messageInput.gameObject.SetActive(false);
        messageInput.ClearInput();
        messageInput.StopTyping();

        ControlsManager.ConfirmClicked -= ConfirmChatMessage;
    }

    public static void ConfirmChatMessage() {
        string message = Instance.messageInput.GetInput();
        SendChatMessage?.Invoke(message);
        
        UiManager.Instance.CloseActiveUiElement();
    }


    public static void ReceiveChatMessage(SChatMessagePkt chatMessageInfo) {
        Instance.messageLog.AddMessage(chatMessageInfo);
    }
}
