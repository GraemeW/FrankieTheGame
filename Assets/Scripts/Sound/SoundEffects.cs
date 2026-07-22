using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using Frankie.Saving;

namespace Frankie.Sound
{
    [RequireComponent(typeof(AudioSource))]
    public class SoundEffects : MonoBehaviour
    {
        // Note:  Functions called via Unity Events, ignore '0 references' messages

        // Tunables
        [SerializeField][Range(0f,2f)] private float additionalVolumeScaler = 1.0f;
        [SerializeField] private List<AudioClip> audioClips = new();
        
        // Const
        private const float _defaultVolume = 0.3f;
        
        // State
        private float volume = _defaultVolume;
        private protected AudioSource audioSource;
        private bool destroyAfterPlay = false;

        #region UnityMethods
        private void Awake()
        {
            InitializeAudioSources();
        }

        private void Start()
        {
            LinkToAudioMixer();
            PreConfigureAudioSource();
        }

        protected virtual void OnEnable()
        {
            InitializeVolume();
        }

        protected virtual void OnDisable()
        {
            // Used in alternate implementations
        }

        private void FixedUpdate()
        {
            if (destroyAfterPlay && audioSource != null && !audioSource.isPlaying)
            {
                Destroy(gameObject);
            }
        }
        #endregion

        #region PrivateProtectedMethods
        private float GetPlayerVolume() => PlayerPrefsController.SoundEffectsVolumeKeyExists() ? Mathf.Clamp01(PlayerPrefsController.GetSoundEffectsVolume() * additionalVolumeScaler) : _defaultVolume;
        
        protected virtual void InitializeAudioSources() => StandardSetAudioSource();
        protected virtual void SetAudioSource(AudioClip audioClip = null) => StandardSetAudioSource();
        protected void StandardSetAudioSource()
        {
            if (audioSource != null) { return; }
            audioSource = GetComponent<AudioSource>();
        }

        protected virtual void LinkToAudioMixer()
        {
            if (CoreAudio.TryGetSoundEffectsAudioMixer(out AudioMixerGroup audioMixerGroup))
            {
                audioSource.outputAudioMixerGroup = audioMixerGroup;
            }
        }

        protected virtual void PreConfigureAudioSource()
        {
            audioSource.Stop();
            if (audioSource.clip != null) { audioSource.time = 0f; }
        }
        
        protected void InitializeVolume()
        {
            if (audioSource == null) { return; }
            volume = GetPlayerVolume();
            audioSource.volume = volume;
        }

        private void GeneratePersistentSoundEffect(AudioClip audioClip)
        {
            if (audioClip == null) { return; }
            SoundEffects newSoundEffects = Instantiate(this, null, true);
            newSoundEffects.InitializeVolume();
            newSoundEffects.destroyAfterPlay = true;
            DontDestroyOnLoad(newSoundEffects);
            newSoundEffects.PlayClip(audioClip);
        }
        #endregion

        #region PublicMethods
        public void SetLooping(bool isLooping)
        {
            if (audioSource == null) { return; }
            audioSource.loop = isLooping;
        }

        public void PlayClip(AudioClip audioClip)
        {
            SetAudioSource(audioClip);
            if (audioSource == null)  { return; }
            if (audioClip == null || audioSource.isPlaying) { return; }
            
            InitializeVolume();
            
            audioSource.Stop();
            audioSource.clip = audioClip;
            audioSource.time = 0f;
            audioSource.Play();
        }

        public void PlayClip()
        {
            if (audioClips.Count == 0) { return; }
            AudioClip audioClip = audioClips[Random.Range(0, audioClips.Count - 1)];
            PlayClip(audioClip);
        }

        public void PlayClipAfterDestroy(AudioClip audioClip)
        {
            if (audioClip == null) { return; }
            GeneratePersistentSoundEffect(audioClip);
        }

        public void PlayClipAfterDestroy(int clipIndex)
        {
            if (audioClips.Count == 0) { return; }
            PlayClipAfterDestroy(audioClips[clipIndex]);
        }

        public void PlayClipAfterDestroy()
        {
            if (audioClips.Count == 0) { return; }
            AudioClip currentClip = audioClips[Random.Range(0, audioClips.Count - 1)];
            PlayClipAfterDestroy(currentClip);
        }
        #endregion
    }
}
