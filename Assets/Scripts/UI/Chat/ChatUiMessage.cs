using Networking.Shared;
using TMPro;
using UnityEngine;

public class ChatUiMessage : MonoBehaviour {
    [SerializeField] private TMP_Text message;
    [SerializeField] private UnityEngine.UI.Image bust;

    [SerializeField] private Sprite playerBust;
    [SerializeField] private Sprite serverBust;

    public void SetChatMessage(WSChatMessagePkt chatMessageInfo) {
        message.text = chatMessageInfo.message;
        message.alpha = 1f;//chatMessageInfo.distanceToSpeaker = 1f - chatMessageInfo.distanceToSpeaker / ChatUiManager.MAX_LOCAL_CHAT_DISTANCE;

        bust.sprite = chatMessageInfo.isServerMessage ? serverBust : playerBust;
    }
}