using UnityEngine;

namespace Audio.Shared {
    public class AudioLookup : BaseLookup<AudioEffect, AudioClip> {
        protected override string ResourcesPath { get => "Audio"; }
    }
}