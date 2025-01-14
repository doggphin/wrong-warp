using UnityEngine;
using UnityEngine.Pool;

namespace Audio.Shared {
    public struct PositionedSoundEffectSettings {
        public AudioEffect audioEffect;
        public Transform transform;
        public Vector3? position;
    }

    public class AudioManager : BaseSingleton<AudioManager> {
        const int MAX_SOUNDS = 32;
        private ObjectPool<AudioPlayer> audioPlayerPool;

        [SerializeField] GameObject audioPlayerPrefab;
        
        protected override void Awake() {
            base.Awake();
            audioPlayerPool = new ObjectPool<AudioPlayer>(OnAudioPlayerPoolCreate, OnAudioPlayerPoolGet, OnAudioPlayerPoolRelease, OnAudioPlayerPoolDestroy, true, MAX_SOUNDS, MAX_SOUNDS);
        }

        public static AudioPlayer PlayPositionedSoundEffect(PositionedSoundEffectSettings audioSettings) {
            AudioPlayer audioPlayer = Instance.audioPlayerPool.Get();
            if(audioPlayer == null)
                return null;

            if(audioSettings.position.HasValue)
                audioPlayer.transform.position = audioSettings.position.Value;
            
            AudioClip clip = AudioLookup.Lookup(audioSettings.audioEffect);
            
            if(clip == null) {
                Instance.audioPlayerPool.Release(audioPlayer);
                return null;
            }   

            audioPlayer.Play(audioSettings.transform, clip);
            return audioPlayer;
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