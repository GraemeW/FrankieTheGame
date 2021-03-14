using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace Frankie.Core
{
    public class BackgroundMusic : MonoBehaviour
    {
        // Tunables
        [SerializeField] float volume = 0.2f;
        [SerializeField] float musicFadeDuration = 3.0f;
        [SerializeField] AudioMixer audioMixer = null;

        // State
        AudioClip currentWorldMusic = null;
        bool isWorldMusicLooping = true;
        float worldMusicTimeIndex = 0f;

        // Cached References
        AudioSource audioSource = null;

        // Static Functions
        private static string MIXER_VOLUME_REFERENCE = "backgroundVolume";

        public static IEnumerator StartFade(AudioMixer audioMixer, string exposedMixedVolumeReference, float duration, float targetVolume)
        {
            float currentTime = 0;
            float currentVol;
            audioMixer.GetFloat(exposedMixedVolumeReference, out currentVol);
            currentVol = Mathf.Pow(10, currentVol / 20);
            float targetValue = Mathf.Clamp(targetVolume, 0.0001f, 1);

            while (currentTime < duration)
            {
                currentTime += Time.deltaTime;
                float newVol = Mathf.Lerp(currentVol, targetValue, currentTime / duration);
                audioMixer.SetFloat(exposedMixedVolumeReference, Mathf.Log10(newVol) * 20);
                yield return null;
            }
            yield break;
        }

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
        }

        private void Start()
        {
            audioSource.outputAudioMixerGroup = audioMixer.outputAudioMixerGroup;
            audioSource.volume = volume;
        }

        public void ConfigureNewWorldAudio(AudioClip audioClip, bool isLooping)
        {
            if (audioClip == null) { return; }

            StartCoroutine(TransitionToAudio(audioClip, isLooping));
            currentWorldMusic = audioClip;
            isWorldMusicLooping = isLooping;
        }

        public void SetBattleMusic(AudioClip audioClip)
        {
            if (audioClip == null) { return; }

            worldMusicTimeIndex = audioSource.time;
            StartCoroutine(TransitionToAudio(audioClip, true));
        }

        public void StopBattleMusic()
        {
            StartCoroutine(TransitionToAudio(currentWorldMusic, isWorldMusicLooping, worldMusicTimeIndex));
        }

        public void SetVolume(float volume)
        {
            this.volume = volume;
            audioSource.volume = volume;
        }

        private IEnumerator TransitionToAudio(AudioClip audioClip, bool isLooping, float timeIndex = 0f)
        {
            yield return StartFade(audioMixer, MIXER_VOLUME_REFERENCE, musicFadeDuration, 0f);
            audioSource.Stop();
            audioSource.clip = audioClip;
            audioSource.loop = isLooping;
            audioSource.time = timeIndex;
            yield return StartFade(audioMixer, MIXER_VOLUME_REFERENCE, musicFadeDuration, volume);
        }

    }

}
