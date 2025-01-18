using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class BaseLookup<EnumIdentifierT, PrefabT> : BaseSingleton<BaseLookup<EnumIdentifierT, PrefabT>>
where EnumIdentifierT : struct, Enum
where PrefabT : UnityEngine.Object {
    private Dictionary<EnumIdentifierT, PrefabT> idToPrefabs = null;

    ///<summary> Override this to select which folder within Assets\Resources this lookup will search for prefabs in </summary>
    protected abstract string ResourcesPath { get; }

    public static PrefabT Lookup(EnumIdentifierT id) => Instance.idToPrefabs[id];

    protected override void Awake() {
        base.Awake();
        InitLookup();
    }

    void InitLookup() {
        var identifierStringsToValues = default(EnumIdentifierT).ToDictionary();

        idToPrefabs = new();
        foreach (var prefab in Resources.LoadAll<PrefabT>(ResourcesPath))
            // Try to match all names of prefabs in given resources folder to enum names...
            if(identifierStringsToValues.TryGetValue(prefab.name, out EnumIdentifierT value))
                // then map those enums to prefabs
                idToPrefabs[value] = prefab;
            else
                Debug.Log($"Could not match file {prefab.name} to a value for {typeof(EnumIdentifierT)}!");

        if(idToPrefabs.Count == identifierStringsToValues.Count)
            return;
        
        foreach(EnumIdentifierT identifier in identifierStringsToValues.Values)
            if(!idToPrefabs.ContainsKey(identifier))
                Debug.LogError($"Could not find a match for {identifier} while searching through {typeof(EnumIdentifierT)}s!");
    }
}