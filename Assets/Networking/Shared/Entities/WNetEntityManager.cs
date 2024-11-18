using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;
using Networking.Shared;
using Unity.VisualScripting;
using Networking.Server;

namespace Networking.Shared {
    public class WNetEntityManager : MonoBehaviour {
        private Dictionary<int, WNetEntity> Entities = new();
        private int nextEntityId = -1;
        private int tick;

        public static WNetEntityManager Instance { get; private set; }
        public static ConcurrentBag<WNetEntity> entitiesToDeleteCache = new();

        private void Awake() {
            if(Instance != null) {
                Destroy(gameObject);
            }
            DontDestroyOnLoad(gameObject);
            Instance = this;
        }

        public WNetEntity SpawnEntity(WNetPrefabId prefabId) {
            WNetEntity ret = Instantiate(WNetPrefabLookup.GetById(prefabId), transform).GetComponent<WNetEntity>();

            // Insert into entities at next available integer
            while (!Entities.TryAdd(++nextEntityId, ret));
            ret.gameObject.name = $"{nextEntityId:0000000000}_{prefabId}";

            return ret;
        }


        public bool KillEntity(int id, WEntityKillReason killReason = WEntityKillReason.Unload) {
            bool exists = Entities.Remove(id, out WNetEntity netEntity);

            if (!exists)
                return false;

            if(WNetManager.IsServer) {
                netEntity.gameObject.SetActive(false);
                netEntity.PushUpdate(WNetServer.Instance.Tick, new WSEntityKillUpdatePkt() { killReason = killReason });
                entitiesToDeleteCache.Add(netEntity);
            }
            else if(WNetManager.IsClient) {
                Destroy(netEntity.gameObject);
            }

            return true;
        }


        public void AdvanceTick(int tick) {
            this.tick = tick;
            foreach (WNetEntity netEntity in Entities.Values) {
                netEntity.Poll(tick);
            }
        }
    }
}