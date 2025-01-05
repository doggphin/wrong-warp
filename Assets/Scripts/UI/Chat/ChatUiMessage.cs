using System.Collections;
using Networking.Shared;
using TMPro;
using UnityEngine;
using SamSharp;
using System;

[RequireComponent(typeof(CanvasGroup), typeof(RectTransform))]
public class ChatUiMessage : MonoBehaviour {
    private const float TimeUntilFade = 2f;
    private const float TimeToFade = 2f;

    [SerializeField] private TMP_Text message;
    [SerializeField] private UnityEngine.UI.Image bust;

    [SerializeField] private Sprite playerBust;
    [SerializeField] private Sprite serverBust;

    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Coroutine fadingCoroutine = null;
    private AudioSource audioSource;

    void Start() {
        canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();
        audioSource = GetComponent<AudioSource>();
        gameObject.SetActive(false);
    }

    public void SetNewMessage(WSChatMessagePkt chatMessagePkt) {
        message.text = chatMessagePkt.message;
        bust.sprite = chatMessagePkt.isServerMessage ? serverBust : playerBust;
        // 40 is base, 5 is padding
        rectTransform.sizeDelta = new Vector2(message.preferredWidth + 40 + 5, rectTransform.sizeDelta.y);

        if(fadingCoroutine != null)
            StopCoroutine(fadingCoroutine);

        gameObject.SetActive(true);
        fadingCoroutine = StartCoroutine(ControlFade());

        GenerateAudio(chatMessagePkt.message);
    }

    IEnumerator ControlFade() {
        canvasGroup.alpha = 1f;

        yield return new WaitForSeconds(TimeUntilFade);
        while(canvasGroup.alpha > 0) {
            canvasGroup.alpha -= Time.deltaTime / TimeToFade;
            yield return null;
        }

        gameObject.SetActive(false);
        fadingCoroutine = null;
    }

    private void GenerateAudio(string msg) {
        Sam sam = new(new Options());
        var byteAudio = sam.Speak(msg);
        
        //var floatAudio = new float[byteAudio.Length / 4];
        //Buffer.BlockCopy(byteAudio, 0, floatAudio, 0, byteAudio.Length);
        var floatAudio = new float[byteAudio.Length];
        for(int i=0; i<floatAudio.Length; i++) {
            floatAudio[i] = (byteAudio[i] / 127f) - 0.5f;
        }
        
        AudioClip newClip = AudioClip.Create("sam", floatAudio.Length, 1, 22050, false);
        newClip.SetData(floatAudio, 0);
        audioSource.clip = newClip;
        audioSource.Play();
    }
}