using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        [Tooltip("Pool for PlayClip - defaults to the attached AudioSource if empty")] [SerializeField] private List<AudioSource> audioSources = new();

        // Const
        private const float _defaultVolume = 0.3f;

        // State
        private readonly List<AudioSource> pooledSources = new();
        private readonly HashSet<AudioSource> allSources = new();
        private bool isAudioMixerLinked = false;
        private Coroutine delayedPlayCoroutine;
        private bool destroyAfterPlay = false;

        #region UnityMethods
        private void Awake()
        {
            InitializeAudioSources();
        }

        private void Start()
        {
            LinkToAudioMixer();
            PreConfigureAudioSources();
        }

        protected virtual void OnEnable()
        {
            InitializeVolume();
        }

        protected virtual void OnDisable()
        {
            if (delayedPlayCoroutine != null) { StopCoroutine(delayedPlayCoroutine); }
        }

        private void FixedUpdate()
        {
            if (destroyAfterPlay && !allSources.Any(source => source != null && source.isPlaying))
            {
                Destroy(gameObject);
            }
        }
        #endregion

        #region SetupMethods
        private float GetPlayerVolume() => PlayerPrefsController.SoundEffectsVolumeKeyExists() ? Mathf.Clamp01(PlayerPrefsController.GetSoundEffectsVolume() * additionalVolumeScaler) : _defaultVolume;
        protected virtual IEnumerable<AudioSource> GetDedicatedAudioSources() => Enumerable.Empty<AudioSource>();
        protected virtual bool TryGetAudioMixerGroup(out AudioMixerGroup audioMixerGroup) => CoreAudio.TryGetSoundEffectsAudioMixer(out audioMixerGroup);

        private void InitializeAudioSources()
        {
            pooledSources.Clear();
            pooledSources.AddRange(audioSources.Where(source => source != null));
            if (pooledSources.Count == 0) { pooledSources.Add(GetComponent<AudioSource>()); } // Note: default self-component only added if pooled is empty (need to add to audioSource list if wanted in pool)

            allSources.Clear();
            allSources.UnionWith(pooledSources);
            allSources.UnionWith(GetDedicatedAudioSources().Where(source => source != null));
        }

        private void LinkToAudioMixer()
        {
            if (isAudioMixerLinked) { return; }
            if (!TryGetAudioMixerGroup(out AudioMixerGroup audioMixerGroup)) { return; }
            foreach (AudioSource audioSource in allSources) { audioSource.outputAudioMixerGroup = audioMixerGroup; }
            isAudioMixerLinked = true;
        }

        protected virtual void PreConfigureAudioSources()
        {
            foreach (AudioSource audioSource in allSources)
            {
                audioSource.Stop();
                if (audioSource.clip != null) { audioSource.time = 0f; }
            }
        }

        protected void InitializeVolume()
        {
            float volume = GetPlayerVolume();
            foreach (AudioSource audioSource in allSources) { audioSource.volume = volume; }
        }

        protected virtual void InitializePersistentSoundEffect()
        {
            PruneToRootAudioSource();
            LinkToAudioMixer(); // Must link immediately after instantiation to ensure set up in time
            InitializeVolume();
            DontDestroyOnLoad(this);
            destroyAfterPlay = false; // Destroy after play set on DelayedPlay
        }

        private void PruneToRootAudioSource()
        {
            AudioSource rootSource = GetComponent<AudioSource>();
            foreach (AudioSource audioSource in allSources)
            {
                if (audioSource == rootSource) { continue; }
                if (audioSource.gameObject == gameObject) { Destroy(audioSource); }
                else if (audioSource.transform.IsChildOf(transform)) { Destroy(audioSource.gameObject); }
            }
            pooledSources.Clear();
            pooledSources.Add(rootSource);
            allSources.Clear();
            allSources.Add(rootSource);
        }
        #endregion

        #region PersistentSoundEffects
        private void GeneratePersistentSoundEffect(AudioClip audioClip)
        {
            if (audioClip == null) { return; }
            SoundEffects newSoundEffects = Instantiate(this, null, true);
            newSoundEffects.InitializePersistentSoundEffect();

            // Note - for reasons, we must delay a frame after instantiation/configuration and before play
            newSoundEffects.StartDelayedPlay(audioClip);
        }

        private void StartDelayedPlay(AudioClip audioClip)
        {
            if (delayedPlayCoroutine != null) { StopCoroutine(delayedPlayCoroutine); }
            delayedPlayCoroutine = StartCoroutine(DelayedPlay(audioClip));
        }

        private IEnumerator DelayedPlay(AudioClip audioClip)
        {
            yield return null;
            destroyAfterPlay = true;
            PlayClip(audioClip);
        }
        #endregion

        #region PrivateMethods
        private bool TryGetIdleAudioSource(AudioClip audioClip, out AudioSource idleSource)
        {
            idleSource = null;
            // Avoid duplicate simultaneous clip plays (impact is otherwise LOUD)
            if (pooledSources.Any(source => source.isPlaying && source.clip == audioClip)) { return false; }
            idleSource = pooledSources.FirstOrDefault(source => !source.isPlaying);
            return idleSource != null;
        }
        #endregion

        #region PublicMethods
        public void SetLooping(bool isLooping)
        {
            foreach (AudioSource audioSource in pooledSources) { audioSource.loop = isLooping; }
        }

        public void PlayClip(AudioClip audioClip)
        {
            if (audioClip == null) { return; }
            if (!TryGetIdleAudioSource(audioClip, out AudioSource audioSource)) { return; }

            InitializeVolume();

            audioSource.Stop();
            audioSource.clip = audioClip;
            audioSource.time = 0f;
            audioSource.Play();
        }

        public void PlayClip()
        {
            if (audioClips.Count == 0) { return; }
            AudioClip audioClip = audioClips[Random.Range(0, audioClips.Count)];
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
            AudioClip currentClip = audioClips[Random.Range(0, audioClips.Count)];
            PlayClipAfterDestroy(currentClip);
        }
        #endregion
    }
}
