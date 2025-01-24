using UnityEngine;

public abstract class Interactable : MonoBehaviour
{
    public abstract string GetHoverText();
    public abstract InteractableIconType GetIconType();
    public abstract void InteractStart();
}
