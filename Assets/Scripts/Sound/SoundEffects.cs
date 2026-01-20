using UnityEngine;
using Frankie.Saving;

namespace Frankie.Sound
{
    [RequireComponent(typeof(AudioSource))]
    public class SoundEffects : MonoBehaviour
    {
        // Note:  Functions called via Unity Events, ignore '0 references' messages

        // Tunables
        [SerializeField] private AudioClip[] audioClips;

        // State
        private float volume = 0.3f;
        private protected AudioSource audioSource;
        private bool destroyAfterPlay = false;

        #region UnityMethods
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
        #endregion

        #region PrivateProtectedMethods
        private void Setup(float defaultVolume, bool setDestroyAfterPlay)
        {
            volume = defaultVolume;
            InitializeVolume();
            destroyAfterPlay = setDestroyAfterPlay;
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

        private void GeneratePersistentSoundEffect(AudioClip audioClip, float defaultVolume)
        {
            if (audioClip == null) { return; }
            SoundEffects newSoundEffects = Instantiate(this, null, true);
            newSoundEffects.Setup(defaultVolume, true);
            DontDestroyOnLoad(newSoundEffects);
            newSoundEffects.PlayClip(audioClip);
        }
        #endregion

        #region PublicMethods
        public void SetLooping(bool isLooping)
        {
            audioSource.loop = isLooping;
        }

        public void PlayClip(AudioClip audioClip)
        {
            if (audioClip == null || audioSource.isPlaying) { return; }
            
            InitializeVolume();
            audioSource.Stop();
            audioSource.clip = audioClip;
            audioSource.time = 0f;
            audioSource.Play();
        }

        public void PlayClip()
        {
            if (audioClips == null) { return; }
            AudioClip audioClip = audioClips[Random.Range(0, audioClips.Length - 1)];
            PlayClip(audioClip);
        }

        public void PlayClipAfterDestroy(AudioClip audioClip)
        {
            if (audioClip == null) { return; }
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
        #endregion
    }
}
