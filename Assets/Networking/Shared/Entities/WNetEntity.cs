using LiteNetLib.Utils;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

namespace Networking.Shared {
    public class WNetEntity : MonoBehaviour {
        public int Id { get; private set; }
        public WNetPrefabId PrefabId { get; private set; }

        [SerializeField]
        private bool s_updatePos, s_updateRot, s_updateScale;
        private Vector3 position;
        private Quaternion rotation;
        private Vector3 scale;

        protected List<INetSerializable>[] s_updatesBuffer = new List<INetSerializable>[WNetCommon.TICKS_PER_UPDATE];
        private bool s_bufferContainsUpdates = false;

        /// <summary>
        /// Outputs the updates attached to this net entity within the last WNetCommon.TICKS_PER_UPDATE ticks.
        /// </summary>
        /// <param name="updates"> The updates this Net Entity had within the last WNetCommon.TICKS_PER_UPDATE ticks, or null if it received nothing. </param>
        /// <returns> Whether any updates were attached to this net entity. </returns>
        public bool GetUpdates(out List<INetSerializable>[] updates) {
            if(!s_bufferContainsUpdates) {
                updates = null;
                return false;
            }

            updates = s_updatesBuffer;
            s_updatesBuffer = new List<INetSerializable>[WNetCommon.TICKS_PER_UPDATE];
            for(int i=0; i< WNetCommon.TICKS_PER_UPDATE; i++) {
                s_updatesBuffer[i] = new();
            }
            s_bufferContainsUpdates = false;
            return true;
        }


        public void Poll(int tick) {
            if (!gameObject.activeInHierarchy)
                return;

            bool hasMoved = s_updatePos && position != transform.position;
            bool hasRotated = s_updateRot && rotation != transform.rotation;
            bool hasScaled = s_updateScale && scale != transform.localScale;

            s_updatesBuffer[tick % WNetCommon.TICKS_PER_UPDATE].Add(new WSEntityTransformUpdatePkt() {
                position = hasMoved ? transform.position : null,
                rotation = hasRotated ? transform.rotation : null,
                scale = hasScaled ? transform.localScale : null
            });
        }


        public void PushUpdate(int tick, INetSerializable packet) {
            s_updatesBuffer[tick % WNetCommon.TICKS_PER_UPDATE].Add(packet);
        }
    }
}

