using UnityEngine;

[CreateAssetMenu(fileName = "EntitySO", menuName = "Scriptable Objects/EntitySO")]
public class EntitySO : ScriptableObject
{
    public GameObject entityPrefab;

    public bool updatePosition = false;
    public bool updateRotation = false;
    public bool updateScale = false;

    public bool hasVelocity = false;
}
