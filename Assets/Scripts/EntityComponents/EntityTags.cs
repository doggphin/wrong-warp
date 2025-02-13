using System.Collections.Generic;
using UnityEngine;

public enum EntityTagType : ushort {
    Interactable,
}

public class EntityTags : MonoBehaviour
{
    private Dictionary<EntityTagType, MonoBehaviour> presentTags = new();

    public void RegisterTag<T>(EntityTagType tag, T component) where T : MonoBehaviour {
        presentTags.TryAdd(tag, component);
    }

    public void UnregisterTag(EntityTagType tag) {
        presentTags.Remove(tag);
    }
}
