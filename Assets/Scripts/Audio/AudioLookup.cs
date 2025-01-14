using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace Audio.Shared {
    public enum AudioEffect {
        Undefined = 0,
        SpellBurst = 1,
    }

    public class AudioLookup : BaseSingleton<AudioLookup> {
        
        private static BaseLookup<AudioClip> baseLookup;

        public static void Init() {
            baseLookup = new();
            baseLookup.Init("Audio");
        }

        public static AudioClip GetById(AudioEffect audioEffect) {
            return baseLookup.GetById((int)audioEffect);
        }
    }
}