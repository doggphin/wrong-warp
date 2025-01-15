using Controllers.Shared;
using UnityEngine;

[RequireComponent(typeof(ObjectViewer))]
public class PlayerViewer : MonoBehaviour {
    private ObjectViewer objectViewer;

    void Awake() {
        objectViewer = GetComponent<ObjectViewer>();
    }

    float rotationsPerSecond = 0.33f;
    void Update() {
        if(ControlsManager.player && ControlsManager.player.TryGetComponent(out ViewablePlayer viewableObject))
            objectViewer.TakeRotatedImage(viewableObject, Time.time * 360 * rotationsPerSecond % 360, 0);
    }
}