using Controllers.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class InteractableUiManager : BaseUiElement<InteractableUiManager> {
    public const float fadeTimeSeconds = 0.15f;
    public override bool RequiresMouse => false;
    public override bool AllowsMovement => true;

    private CanvasGroup canvasGroup;
    [SerializeField] TMP_Text displayText;
    [SerializeField] Image icon;

    BaseInteractable lastSeenInteractable = null;

    protected override void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        base.Awake();
        gameObject.SetActive(true);
    }

    void LateUpdate() {
        if(!ControlsManager.TryGetPlayer(out var player)) {
            canvasGroup.alpha = 0;
            return;
        }

        float alphaDelta = Time.deltaTime / fadeTimeSeconds;
        if(!player.PollForInteractable(out var interactable)) {
            canvasGroup.alpha -= alphaDelta;
            return;
        }

        if(!ReferenceEquals(interactable, lastSeenInteractable)) {
            icon.sprite = InteractableIconLookup.Lookup(interactable.GetIconType());
            lastSeenInteractable = interactable;
        }

        displayText.text = interactable.GetHoverText();
        
        canvasGroup.alpha += alphaDelta;
    }
}