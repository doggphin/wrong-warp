using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class BaseLookup<EnumIdentifierT, PrefabT> : BaseSingleton<BaseLookup<EnumIdentifierT, PrefabT>>
where EnumIdentifierT : struct, Enum
where PrefabT : UnityEngine.Object {
    private Dictionary<EnumIdentifierT, PrefabT> idToPrefabs = null;

    protected abstract string ResourcesPath { get; }

    public static PrefabT Lookup(EnumIdentifierT id) => Instance.idToPrefabs[id];

    protected override void Awake() {
        base.Awake();
        
        idToPrefabs = new();
        
        // Gets all the string names and values for a given enum
        var identifierStrings = Enum.GetNames(typeof(EnumIdentifierT));
        var identifierValues = (EnumIdentifierT[])Enum.GetValues(typeof(EnumIdentifierT));
        var identifierStringsToValues = new Dictionary<string, EnumIdentifierT>();
        for(int i=0; i<identifierStrings.Length; i++) {
            identifierStringsToValues[identifierStrings[i]] = identifierValues[i];
        }

        PrefabT[] loadedPrefabs = Resources.LoadAll<PrefabT>(ResourcesPath);

        // Try to match all names of prefabs in given resources folder to enum strings, then map those enums to prefabs
        int successfulMatches = 0;
        foreach (var prefab in loadedPrefabs) {
            if(!identifierStringsToValues.TryGetValue(prefab.name, out EnumIdentifierT value)) {
                Debug.Log($"Could not match file {prefab.name} to a value for {typeof(EnumIdentifierT)}!");
                continue;
            }
            successfulMatches++;
            idToPrefabs[value] = prefab;
        }

        // If there were any unmatched enum values, errors could be caused trying to get them -- log errors
        if(successfulMatches == identifierValues.Length)
            return;
        
        foreach(EnumIdentifierT identifier in identifierValues) {
            if(!idToPrefabs.ContainsKey(identifier)) {
                Debug.LogError($"Could not find a match for {identifier} while searching through {typeof(EnumIdentifierT)}s!");
            }
        }
    }
}