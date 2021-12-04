using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frankie.Settings;

namespace Frankie.Sound
{
    [RequireComponent(typeof(AudioSource))]
    public class SoundEffects : MonoBehaviour
    {
        // Note:  Functions called via Unity Events, ignore '0 references' messages

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

        public void Setup(float defaultVolume, bool destroyAfterPlay)
        {
            volume = defaultVolume;
            InitializeVolume();
            this.destroyAfterPlay = destroyAfterPlay;
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

        protected void GeneratePersistentSoundEffect(AudioClip audioClip, float defaultVolume)
        {
            if (audioClip == null) { return; }
            SoundEffects newSoundEffects = Instantiate(this);
            newSoundEffects.transform.parent = null;
            newSoundEffects.Setup(defaultVolume, true);
            DontDestroyOnLoad(newSoundEffects);
            newSoundEffects.PlayClip(audioClip);
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
            AudioClip audioClip = audioClips[Random.Range(0, audioClips.Length - 1)];
            PlayClip(audioClip);
        }

        public void PlayClipAfterDestroy(AudioClip audioClip)
        {
            if (audioClips == null) { return; }
            GeneratePersistentSoundEffect(audioClip, audioSource.volume);
        }

        public void PlayClipAfterDestroy(int clipIndex)
        {
            if (audioClips == null) { return; }
            PlayClipAfterDestroy(audioClips[clipIndex]);
        }

        public void PlayClipAfterDestroy()
        {
            if (audioClips == null) { return; }
            AudioClip currentClip = audioClips[Random.Range(0, audioClips.Length - 1)];
            PlayClipAfterDestroy(currentClip);
        }
    }
}