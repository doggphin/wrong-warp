using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Networking.Shared {
    public static class WPrefabLookup {

        static Dictionary<WPrefabId, GameObject> idToNetPrefabs = null;

        static readonly string shortNetPrefabPath = Path.Combine("NetPrefabs");
        static readonly string netPrefabPath = Path.Combine(".", "Assets", "Resources", "NetPrefabs");

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
