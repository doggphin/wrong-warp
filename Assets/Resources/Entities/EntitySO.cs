using UnityEngine;

[CreateAssetMenu(fileName = "EntitySO", menuName = "Scriptable Objects/EntitySO")]
public class EntitySO : ScriptableObject
{
    public GameObject entityPrefab;

    [Space(10)]
    public bool updatePositionOverNetwork = false;
    public bool updateRotationOverNetwork = false;
    public bool updateScaleOverNetwork = false;

    [Space(10)]
    public AutomaticMovementType autoMovementType = AutomaticMovementType.None;
}

public enum AutomaticMovementType {
    None,
    Velocity,
    Rigidbody,
}
