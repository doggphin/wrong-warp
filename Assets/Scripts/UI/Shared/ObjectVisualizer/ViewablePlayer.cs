using UnityEngine;

[System.Serializable]
public class ViewablePlayerParts {
    public Transform head;
    public Transform body;
    public Transform legs;
    public Transform feet;
}

public class ViewablePlayer : ViewableObject {
    [SerializeField] private ViewablePlayerParts playerParts;

    public ViewablePlayerParts PlayerParts => playerParts;
}