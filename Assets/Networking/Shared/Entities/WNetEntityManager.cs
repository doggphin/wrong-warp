using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;
using Networking.Shared;

namespace Networking.Shared {
    public class WNetEntityManager : MonoBehaviour {
        Dictionary<int, WNetEntity> entities;
        int nextEntityId;

        public static WNetEntityManager Instance { get; private set; }

        private void Awake() {
            if (Instance != null && Instance != this)
                Destroy(gameObject);
            else {
                Instance = this;
                WNetPrefabLookup.Init();
            }
                
                
        }

        public static WNetEntity SpawnEntity(WNetPrefabId prefabId) {
            WNetEntity ret = Instantiate(WNetPrefabLookup.GetById(prefabId)).GetComponent<WNetEntity>();

            // Insert into entities at next available integer
            while (!Instance.entities.TryAdd(Instance.nextEntityId++, ret));

            return ret;
        }
    }
}