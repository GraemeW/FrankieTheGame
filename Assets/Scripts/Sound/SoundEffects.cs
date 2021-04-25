using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frankie.Settings;

namespace Frankie.Sound
{
    public class SoundEffects : MonoBehaviour
    {
        // Tunables
        [SerializeField] AudioClip[] audioClips = null;

        // State
        float volume = 0.3f;
        protected AudioSource audioSource = null;
        bool destroyAfterPlay = false;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
        }

        protected virtual void OnEnable()
        {
            InitializeVolume();
        }

        protected virtual void OnDisable()
        {
            // Used in alternate implementations
        }

        protected virtual void Update()
        {
            if (destroyAfterPlay && !audioSource.isPlaying)
            {
                Destroy(gameObject);
            }
        }

        protected void InitializeVolume()
        {
            if (PlayerPrefsController.MasterVolumeKeyExists())
            {
                if (PlayerPrefsController.SoundEffectsVolumeKeyExists())
                {
                    volume = PlayerPrefsController.GetMasterVolume() * PlayerPrefsController.GetSoundEffectsVolume();
                }
                else
                {
                    volume = PlayerPrefsController.GetMasterVolume();
                }
            }
            audioSource.volume = volume;
        }

        public void SetLooping(bool isLooping)
        {
            audioSource.loop = isLooping;
        }

        public void PlayClip(AudioClip audioClip)
        {
            if (audioClip == null) { return; }
            InitializeVolume();
            audioSource.clip = audioClip;
            if (!audioSource.isPlaying) { audioSource.Play(); }
        }

        public void PlayClip()
        {
            if (audioClips == null) { return; }
            InitializeVolume();
            audioSource.clip = audioClips[Random.Range(0, audioClips.Length - 1)];
            if (!audioSource.isPlaying) { audioSource.Play(); }
        }

        public void PlayClipAfterDestroy(AudioClip audioClip)
        {
            if (audioClips == null) { return; }
            InitializeVolume();
            AudioSource.PlayClipAtPoint(audioClip, Camera.main.transform.position, audioSource.volume);
        }

        public void PlayClipAfterDestroy(int clipIndex)
        {
            if (audioClips == null) { return; }
            InitializeVolume();
            AudioSource.PlayClipAtPoint(audioClips[clipIndex], Camera.main.transform.position, audioSource.volume);
        }

        public void PlayClipAfterDestroy()
        {
            if (audioClips == null) { return; }
            InitializeVolume();
            AudioClip currentClip = audioClips[Random.Range(0, audioClips.Length - 1)];
            AudioSource.PlayClipAtPoint(currentClip, Camera.main.transform.position, audioSource.volume);
        }

        public void PlayRandomClipAndPersistOnSceneTransition()
        {
            if (audioClips == null) { return; }
            InitializeVolume();
            DontDestroyOnLoad(gameObject);
            PlayClip();
            destroyAfterPlay = true;
        }

    }
}