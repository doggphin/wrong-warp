using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;
using Networking.Shared;
using Unity.VisualScripting;
using Networking.Server;
using UnityEditor.VersionControl;

namespace Networking.Shared {
    public class WNetEntityManager : MonoBehaviour {
        private Dictionary<int, WNetEntity> Entities { get; set; } = new();
        private int nextEntityId = -1;
        private int tick;

        public static WNetEntityManager Instance { get; private set; }
        public static HashSet<WNetEntity> entitiesToAddCache = new();
        public static HashSet<WNetEntity> entitiesToDeleteCache = new();

        private void Awake() {
            if(Instance != null) {
                Destroy(gameObject);
            }
            DontDestroyOnLoad(gameObject);
            Instance = this;
        }

        public WNetEntity SpawnEntity(WNetPrefabId prefabId, bool isChunkLoader = false) {
            WNetEntity ret = Instantiate(WNetPrefabLookup.GetById(prefabId), transform).GetComponent<WNetEntity>();

            // Insert into entities at next available integer
            while (Entities.ContainsKey(++nextEntityId));
            Debug.Log($"Spawned {nextEntityId}!");

            entitiesToAddCache.Add(ret);
            ret.gameObject.name = $"{nextEntityId:0000000000}_{prefabId}";

            ret.InitServer(isChunkLoader);

            return ret;
        }


        public static bool KillEntity(int id, WEntityKillReason killReason = WEntityKillReason.Unload) {
            Debug.Log($"Killing {id}!");

            if (!Instance.Entities.TryGetValue(id, out WNetEntity netEntity))
                return false;

            netEntity.Kill(killReason);
            if (WNetManager.IsServer)
                entitiesToDeleteCache.Add(netEntity);

            else if(WNetManager.IsClient)
                Destroy(netEntity.gameObject);

            return true;
        }


        public void AdvanceTick(int tick) {
            this.tick = tick;

            foreach (var entity in entitiesToDeleteCache) {
                Entities.Remove(entity.Id);
            }
            entitiesToDeleteCache.Clear();

            foreach (var entity in entitiesToAddCache) {
                Entities.Add(entity.Id, entity);
            }
            entitiesToAddCache.Clear();

            foreach (WNetEntity netEntity in Entities.Values) {
                netEntity.Poll(tick);
            }
        }
    }
}