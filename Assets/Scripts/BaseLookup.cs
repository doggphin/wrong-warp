using System.Collections.Generic;
using UnityEngine;

public class BaseLookup<PrefabType> where PrefabType : UnityEngine.Object {
    private Dictionary<int, PrefabType> idToItems = null;

    private bool isInitialized = false;
    public void Init(string prefabPath) {
        if(isInitialized)
            return;
        
        idToItems = new();
        
        PrefabType[] loadedPrefabs = Resources.LoadAll<PrefabType>(prefabPath);

        foreach (var prefab in loadedPrefabs) {
            Debug.Log(prefab.name);
            string[] splitFileName = prefab.name.Split('_');
            if (splitFileName.Length != 2) {
                Debug.LogWarning($"Invalid prefab name format: {prefab.name}");
                continue;
            }

            if (int.TryParse(splitFileName[0], out int id)) {
                idToItems[id] = prefab;
            } else {
                Debug.LogWarning($"Invalid ID in prefab name: {prefab.name}");
            }
        }

        isInitialized = true;
    }

    public PrefabType GetById(int id) {
        if (idToItems.TryGetValue(id, out var prefab))
            return prefab;

        Debug.LogError($"Prefab with ID {id} not found!");
        return default;
    }
}