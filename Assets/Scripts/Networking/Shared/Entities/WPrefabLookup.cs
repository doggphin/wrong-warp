using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Networking.Shared {
    public enum WPrefabId : int
    {
        Undefined = 0,
        Test = 1,
        Player = 2,
        Spectator = 3,
    }


    public struct WPrefabTransformUpdateTypes {
        public bool updatePosition;
        public bool updateRotation;
        public bool updateScale;

        public WPrefabTransformUpdateTypes(bool updatePosition, bool updateRotation, bool updateScale) {
            this.updatePosition = updatePosition;
            this.updateRotation = updateRotation;
            this.updateScale = updateScale;
        }
    }


    public static class WPrefabLookup {
        public static Dictionary<WPrefabId, WPrefabTransformUpdateTypes> PrefabUpdateTypes { get; private set; } = new() {
            { WPrefabId.Undefined, new WPrefabTransformUpdateTypes(false, false, false) },
            { WPrefabId.Test, new WPrefabTransformUpdateTypes(true, false, false) },
            { WPrefabId.Player, new WPrefabTransformUpdateTypes(true, true, false) },
            { WPrefabId.Spectator, new WPrefabTransformUpdateTypes(true, true, false) }
        };

        private static Dictionary<WPrefabId, GameObject> idToNetPrefabs = null;

        private const string netPrefabPath = "NetPrefabs";

        public static void Init() {
            idToNetPrefabs = new Dictionary<WPrefabId, GameObject>();

            // Load all prefabs in the "Resources/NetPrefabs" folder
            GameObject[] loadedPrefabs = Resources.LoadAll<GameObject>(netPrefabPath);

            foreach (var prefab in loadedPrefabs) {
                string[] splitFileName = prefab.name.Split('_');
                if (splitFileName.Length != 2) {
                    Debug.LogWarning($"Invalid prefab name format: {prefab.name}");
                    continue;
                }

                if (int.TryParse(splitFileName[0], out int id)) {
                    idToNetPrefabs[(WPrefabId)id] = prefab;
                } else {
                    Debug.LogWarning($"Invalid ID in prefab name: {prefab.name}");
                }
            }
        }

        public static GameObject GetById(WPrefabId prefabId) {
            if (idToNetPrefabs.TryGetValue(prefabId, out var prefab)) {
                return prefab;
            }

            Debug.LogError($"Prefab with ID {prefabId} not found!");
            return null;
        }
    }
}
