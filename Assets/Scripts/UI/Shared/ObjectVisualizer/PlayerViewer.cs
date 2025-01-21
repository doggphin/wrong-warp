using Controllers.Shared;
using UnityEngine;

[RequireComponent(typeof(ObjectViewer))]
public class PlayerViewer : MonoBehaviour {
    private ObjectViewer objectViewer;

    void Awake() {
        objectViewer = GetComponent<ObjectViewer>();
    }

    float rotationsPerSecond = 0.33f;
    void LateUpdate() {
        Capture();
    }

    void OnRectTransformDimensionsChange() {
        Capture();
    }

    void Capture() {
        if(ControlsManager.TryGetPlayer(out var player) &&
        player.TryGetComponent(out ViewablePlayer viewableObject)) {
            objectViewer.TakeRotatedImage(viewableObject, Time.time * 360 * rotationsPerSecond % 360, 0);
        }      
    }
}