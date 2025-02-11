using Inventories;
using UnityEngine;

[CreateAssetMenu(fileName = "EntitySO", menuName = "Scriptable Objects/EntitySO")]
public class EntitySO : ScriptableObject
{
    [field: SerializeField] public GameObject EntityPrefab { get; private set; }

    [field: SerializeField] public bool UpdatePositionOverNetwork { get; private set; }
    [field: SerializeField] public bool UpdateRotationOverNetwork { get; private set; }
    [field: SerializeField] public bool UpdateScaleOverNetwork { get; private set; }

    [field: SerializeField] public InventoryTemplateSO InventoryTemplate { get; private set; }
}
