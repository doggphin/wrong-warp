using TriInspector;
using UnityEngine;

[System.Serializable]
public class ViewablePlayerParts {
    public Transform head;
    public Transform body;
    public Transform legs;
    public Transform feet;
}

public class ViewablePlayer : ViewableObject {
    [field : SerializeField] public ViewablePlayerParts PlayerParts { get; private set; } = new();
}