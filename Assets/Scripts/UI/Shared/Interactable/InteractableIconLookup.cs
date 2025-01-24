using UnityEngine;

public enum InteractableIconType
{
    Take,
}

public class InteractableIconLookup : BaseLookup<InteractableIconType, Sprite> {
    protected override string ResourcesPath { get => "InteractableIcons"; }
}