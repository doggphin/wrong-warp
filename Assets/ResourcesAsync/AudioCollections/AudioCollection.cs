using Inventories;
using UnityEngine;

[CreateAssetMenu(fileName = "RandomAudioCollectionSO", menuName = "Scriptable Objects/RandomAudioCollectionSO")]
public class AudioCollection : ScriptableObject
{
    public struct PlayableAudio {
        public AudioClip clip;
        public float range;
        public float pitch;
    }

    [SerializeField] private AudioClip[] Clips;
    [SerializeField] private float Range;
    [SerializeField] private float PitchVariation;

    public PlayableAudio GetRandomSoundChoice() {
        return new PlayableAudio() {
            clip = Clips[Random.Range(0, Clips.Length)],
            range = Range,
            pitch = 1 + (Random.Range(-PitchVariation, PitchVariation) / 100)
        };
    }
}
