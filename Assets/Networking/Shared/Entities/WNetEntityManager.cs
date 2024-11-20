using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;
using Networking.Shared;
using Unity.VisualScripting;
using Networking.Server;
using UnityEditor.VersionControl;
using System.Linq;

namespace Networking.Shared {
    public class WNetEntityManager : MonoBehaviour {
        private Dictionary<int, WNetEntity> Entities { get; set; } = new();
        private int nextEntityId = -1;
        private int tick;

        public static WNetEntityManager Instance { get; private set; }

        private void Awake() {
            if(Instance != null) {
                Destroy(gameObject);
            }
            DontDestroyOnLoad(gameObject);
            Instance = this;
        }

        public static WNetEntity SpawnEntity(WNetPrefabId prefabId, bool isChunkLoader = false) {
            WNetEntity ret = Instantiate(WNetPrefabLookup.GetById(prefabId), Instance.transform).GetComponent<WNetEntity>();

            // Insert into entities at next available integer
            while (!Instance.Entities.TryAdd(++Instance.nextEntityId, ret));

            Debug.Log($"Spawned {Instance.nextEntityId}!");
            ret.gameObject.name = $"{Instance.nextEntityId:0000000000}_{prefabId}";
            ret.InitServer(isChunkLoader);

            return ret;
        }


        public static bool KillEntity(int id, WEntityKillReason killReason = WEntityKillReason.Unload) {
            Debug.Log($"Killing {id}!");

            if (!Instance.Entities.TryGetValue(id, out WNetEntity netEntity))
                return false;

            netEntity.Kill(killReason);
            if (WNetManager.IsServer)
                Instance.Entities.Remove(id);

            else if(WNetManager.IsClient)
                Destroy(netEntity.gameObject);

            return true;
        }


        public void AdvanceTick(int tick) {
            this.tick = tick;

            // TODO: This could be done more efficiently
            foreach (WNetEntity netEntity in Entities.Values.ToList()) {
                netEntity.Poll(tick);
            }
        }
    }
}