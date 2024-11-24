using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Networking.Shared {
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
        public static Dictionary<WPrefabId, WPrefabTransformUpdateTypes> PrefabUpdateTypes {get; private set;} = new() {
            { WPrefabId.Empty, new WPrefabTransformUpdateTypes(false, false, false) },
            { WPrefabId.Test, new WPrefabTransformUpdateTypes(true, false, false) },
            { WPrefabId.Player, new WPrefabTransformUpdateTypes(true, true, false) },
            { WPrefabId.Spectator, new WPrefabTransformUpdateTypes(true, true, false) }
        };

        private static Dictionary<WPrefabId, GameObject> idToNetPrefabs = null;

        private static readonly string shortNetPrefabPath = Path.Combine("NetPrefabs");
        private static readonly string netPrefabPath = Path.Combine(".", "Assets", "Resources", "NetPrefabs");

        public static void Init() {
            idToNetPrefabs = new();

            foreach (string filePath in Directory.GetFileSystemEntries(netPrefabPath, "*.prefab")) {
                string fileName = filePath.Split(Path.DirectorySeparatorChar).Last();

                string[] split = fileName.Split('_');
                if(split.Length != 2) {
                    continue;
                }

                int id = int.Parse(split[0]);
                string gameObjectPath = Path.Combine(shortNetPrefabPath, fileName).Split('.')[0];

                idToNetPrefabs[(WPrefabId)id] = Resources.Load<GameObject>(gameObjectPath);
            }
        }

        public static GameObject GetById(WPrefabId prefabId) {   
            return idToNetPrefabs[prefabId];
        }
    }
}
