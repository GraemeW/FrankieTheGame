using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frankie.Settings;

namespace Frankie.Core
{
    public class SoundEffects : MonoBehaviour
    {
        // Tunables
        [SerializeField] AudioClip[] audioClips = null;

        // State
        float volume = 0.3f;
        AudioSource audioSource = null;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
        }

        private void Start()
        {
            if (PlayerPrefsController.VolumeKeyExist())
            {
                volume = PlayerPrefsController.GetMasterVolume();
            }
            audioSource.volume = volume;
        }

        public void PlayRandomClip()
        {
            if (audioClips == null) { return; }
            audioSource.clip = audioClips[Random.Range(0, audioClips.Length - 1)];
            if (!audioSource.isPlaying) { audioSource.Play(); }
        }

        public void PlayClipAfterDestroy(int clipIndex)
        {
            if (audioClips == null) { return; }
            AudioSource.PlayClipAtPoint(audioClips[clipIndex], transform.position, audioSource.volume);
        }

        public void PlayRandomClipAfterDestroy()
        {
            if (audioClips == null) { return; }
            AudioClip currentClip = audioClips[Random.Range(0, audioClips.Length - 1)];
            AudioSource.PlayClipAtPoint(currentClip, transform.position, audioSource.volume);
        }
    }
}