using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Networking.Shared {
    public class NetPrefabLookup : BaseLookup<NetPrefabType, GameObject> {
        protected override string ResourcesPath { get => "NetPrefabs"; }

        public static Dictionary<NetPrefabType, WPrefabTransformUpdateTypes> PrefabUpdateTypes { get; private set; } = new() {
            { NetPrefabType.Test, new WPrefabTransformUpdateTypes(true, false, false) },
            { NetPrefabType.Player, new WPrefabTransformUpdateTypes(true, true, false) },
            { NetPrefabType.Spectator, new WPrefabTransformUpdateTypes(true, true, false) }
        };
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
}
