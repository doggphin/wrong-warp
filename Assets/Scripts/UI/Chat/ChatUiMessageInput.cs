using System;
using Controllers.Shared;
using TMPro;
using UnityEngine;

public class ChatUiMessageInput : MonoBehaviour {
    [SerializeField] TMP_InputField input;

    public void StartTyping() {
        input.Select();
        input.ActivateInputField();

        input.onEndEdit.RemoveListener(ReselectInputField);
    }

    public void StopTyping() {
        input.DeactivateInputField();
        input.onEndEdit.RemoveListener(ReselectInputField);
    }

    private void ReselectInputField(string _) {
        Debug.Log("Reselecting input!");
        input.Select();
    }


    public string GetInput() {
        return input.text;
    }


    public void ClearInput() {
        input.text = "";
    }
}