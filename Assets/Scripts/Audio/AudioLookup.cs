using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace Audio.Shared {
    public class AudioLookup : BaseLookup<AudioEffect, AudioClip> {
        protected override string ResourcesPath { get => "Audio"; }
    }
}