using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace Audio.Shared {
    [RequireComponent(typeof(AudioSource))]
    public class AudioPlayer : MonoBehaviour {
        public Action<AudioPlayer> FinishedPlaying;

        private AudioSource audioSource;

        void Awake() {
            audioSource = GetComponent<AudioSource>();
        }

        public void Stop() {
            audioSource.Stop();
            StopAllCoroutines();
            FinishedPlaying?.Invoke(this);
        }

        private IEnumerator PlayAtSourceThenRelease(Transform parentTransform, AudioClip clip) {
            audioSource.clip = clip;
            audioSource.Play();

            if(parentTransform == null) {
                gameObject.name = clip.name;
                yield return new WaitForSeconds(clip.length);
            } else {
                gameObject.name = $"{parentTransform.gameObject.name} - {clip.name}";
                for(float elapsed = 0; elapsed <= clip.length; elapsed += Time.deltaTime) {
                    transform.position = parentTransform.position;
                    yield return null;
                }
            }

            Stop();
        }

        public void Play(Transform parentTransform, AudioClip clip) {
            StopAllCoroutines();   
            StartCoroutine(PlayAtSourceThenRelease(parentTransform, clip));
        }

        void OnDestroy() {
            Stop();
        }
    }
}