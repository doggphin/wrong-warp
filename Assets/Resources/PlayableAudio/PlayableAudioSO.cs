using Inventories;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayableAudioSO", menuName = "Scriptable Objects/PlayableAudioSO")]
public class PlayableAudioSO : ScriptableObject
{
    [field: SerializeField] public AudioClip Clip { get; private set; }
    [field: SerializeField] public float Range { get; private set; }
    [field: SerializeField] public float PitchVariation { get; private set; }
}
