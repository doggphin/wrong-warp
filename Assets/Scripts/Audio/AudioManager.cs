using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEditor.Timeline.Actions;
using UnityEngine;
using UnityEngine.Pool;

namespace Audio.Shared {
    public class AudioManager : BaseSingleton<AudioManager> {
        const int MAX_SOUNDS = 128;
        private ObjectPool<AudioPlayer> audioPlayerPool;

        [SerializeField] GameObject audioPlayerPrefab;
        
        protected override void Awake() {
            base.Awake();
            audioPlayerPool = new ObjectPool<AudioPlayer>(OnAudioPlayerPoolCreate, OnAudioPlayerPoolGet, OnAudioPlayerPoolRelease, OnAudioPlayerPoolDestroy, true, MAX_SOUNDS, MAX_SOUNDS);
        }


        public static void PlayPositionedSoundEffect(string audioFile, Transform t) {
            AsyncAudioLookup.TryGetAsset(audioFile, 
                (randomAudio) => Instance.PlayPositionedSoundEffect(randomAudio, t));
        }


        private void PlayPositionedSoundEffect(RandomAudioCollectionSO randomAudio, Transform t) {
            AudioPlayer audioPlayer = Instance.audioPlayerPool.Get();

            PlayableAudioSO randomAudioChoice = randomAudio.GetRandomSoundChoice();
            audioPlayer.Play(t, randomAudioChoice.Clip);
        }


        private AudioPlayer OnAudioPlayerPoolCreate() {
            AudioPlayer audioPlayer = Instantiate(audioPlayerPrefab, transform).GetComponent<AudioPlayer>();
            return audioPlayer;
        }


        private void OnAudioPlayerPoolRelease(AudioPlayer audioPlayer) {
            audioPlayer.gameObject.SetActive(false);
        }

        private void OnAudioPlayerPoolGet(AudioPlayer audioPlayer) {
            audioPlayer.gameObject.SetActive(true);
            audioPlayer.FinishedPlaying += ReturnAudioPlayerToPool;
        }

        private void ReturnAudioPlayerToPool(AudioPlayer audioPlayer) {
            audioPlayer.FinishedPlaying -= ReturnAudioPlayerToPool;
            audioPlayerPool.Release(audioPlayer);
        }

        private void OnAudioPlayerPoolDestroy(AudioPlayer audioPlayer) {
            Destroy(audioPlayer.gameObject);
        }
    }
}