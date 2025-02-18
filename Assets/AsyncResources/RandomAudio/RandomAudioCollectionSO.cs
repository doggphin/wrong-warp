using Inventories;
using UnityEngine;

[CreateAssetMenu(fileName = "RandomAudioCollectionSO", menuName = "Scriptable Objects/RandomAudioCollectionSO")]
public class RandomAudioCollectionSO : ScriptableObject
{
    [field: SerializeField] public PlayableAudioSO[] AudioChoices { get; private set; }

    public PlayableAudioSO GetRandomSoundChoice() {
        return AudioChoices[Random.Range(0, AudioChoices.Length)];
    }
}
