using Controllers.Shared;
using UnityEditor.Localization.Plugins.XLIFF.V12;
using UnityEngine;

[RequireComponent(typeof(ObjectViewer))]
public class PlayerViewer : MonoBehaviour {
    ///<summary> Represents screen space positions of different player parts </summary>
    public struct PlayerPartRectPositions {
        public Vector2 head, body, legs, feet;

        // Sets head, body, legs and feet to their screen space positions; essentially takes a screenshot
        public PlayerPartRectPositions(ViewablePlayer viewablePlayer, ObjectViewer objectViewer) {
            Vector2 SetPosition(Transform playerPartTransform) => objectViewer.GetScreenSpacePosition(playerPartTransform.position);

            ViewablePlayerParts playerParts = viewablePlayer.PlayerParts;

            head = SetPosition(playerParts.head);
            body = SetPosition(playerParts.body);
            legs = SetPosition(playerParts.legs);
            feet = SetPosition(playerParts.feet);
        }
    }


    private ObjectViewer objectViewer;
    public PlayerPartRectPositions CurrentPlayerPartWorldSpacePositions { get; private set; }


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
        if(ControlsManager.TryGetPlayer(out var player) && player.TryGetComponent(out ViewablePlayer viewablePlayer)) {
            objectViewer.TakeRotatedImage(viewablePlayer, Time.time * 360 * rotationsPerSecond % 360, 0);
            CurrentPlayerPartWorldSpacePositions = new(viewablePlayer, objectViewer);
        }
    }
}