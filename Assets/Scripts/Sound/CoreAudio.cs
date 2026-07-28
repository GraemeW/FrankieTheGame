using UnityEngine;
using UnityEngine.Audio;
using Frankie.Saving;

namespace Frankie.Sound
{
    public class CoreAudio : MonoBehaviour
    {
        [Header("Defaults")]
        [SerializeField] private float defaultMasterVolume = 0.5f;
        [SerializeField] private float defaultBackgroundMusicVolume = 0.5f;
        [SerializeField] private float defaultSoundEffectsVolume = 0.5f;
        [Header("Hookups")]
        [SerializeField] private AudioMixerGroup masterAudioMixer;
        [SerializeField] private AudioMixerGroup backgroundAudioMixer;
        [SerializeField] private AudioMixerGroup soundEffectsAudioMixer;
        [SerializeField] private AudioMixerGroup faderAudioMixer;
        
        // Fixed Constants
        private const float _minVolume = 0.0001f; // -80dB
        private const string _coreAudioTag = "CoreAudio";
        private const string _backgroundMusicTag = "BackgroundMusic";
        private const string _mixerVolumeReference = "masterVolume";
        
        // Static State
        private static CoreAudio _coreAudioInstance;
        private static BackgroundMusic _backgroundMusicInstance;
        
        // Instance State
        private float volume = 0.4f;

        #region StaticFinders
        private static CoreAudio FindCoreAudioInstance()
        {
            var coreVolumeGameObject = GameObject.FindGameObjectWithTag(_coreAudioTag);
            return coreVolumeGameObject != null ? coreVolumeGameObject.GetComponent<CoreAudio>() : null;
        }
        
        private static BackgroundMusic FindBackgroundMusic()
        {
            var backgroundMusicGameObject = GameObject.FindGameObjectWithTag(_backgroundMusicTag);
            return backgroundMusicGameObject != null ? backgroundMusicGameObject.GetComponent<BackgroundMusic>() : null;
        }
        #endregion
        
        #region StaticGetters
        public static bool DoesBackgroundMusicExist()
        {
            if (_backgroundMusicInstance == null) { _backgroundMusicInstance = FindBackgroundMusic(); }
            return _backgroundMusicInstance != null;
        }

        private static bool DoesCoreAudioExist()
        {
            if (_coreAudioInstance == null) { _coreAudioInstance = FindCoreAudioInstance(); }
            return _coreAudioInstance != null;
        }
        
        public static bool TryGetMasterAudioMixer(out AudioMixerGroup audioMixerGroup)
        {
            audioMixerGroup = null;
            if (!DoesCoreAudioExist()) { return false; }
            audioMixerGroup = _coreAudioInstance.masterAudioMixer;
            return true;
        }
        public static bool TryGetBackgroundAudioMixer(out AudioMixerGroup audioMixerGroup)
        {
            audioMixerGroup = null;
            if (!DoesCoreAudioExist()) { return false; }
            audioMixerGroup = _coreAudioInstance.backgroundAudioMixer;
            return true;
        }
        public static bool TryGetSoundEffectsAudioMixer(out AudioMixerGroup audioMixerGroup)
        {
            audioMixerGroup = null;
            if (!DoesCoreAudioExist()) { return false; }
            audioMixerGroup = _coreAudioInstance.soundEffectsAudioMixer;
            return true;
        }
        
        public static bool TryGetFaderAudioMixer(out AudioMixerGroup audioMixerGroup)
        {
            audioMixerGroup = null;
            if (!DoesCoreAudioExist()) { return false; }
            audioMixerGroup = _coreAudioInstance.faderAudioMixer;
            return true;
        }
        #endregion
        
        #region StaticSetters
        public static void RefreshMasterVolume()
        {
            if (!DoesCoreAudioExist()) { return; }
            _coreAudioInstance.RefreshVolume();
        }

        public static void RefreshBackgroundMusicVolume()
        {
            if (!DoesBackgroundMusicExist()) { return; }
            _backgroundMusicInstance.RefreshVolume();
        }

        public static bool OverrideBackgroundMusic(AudioClip audioClip)
        {
            if (!DoesBackgroundMusicExist() || audioClip == null) { return false; }
            return _backgroundMusicInstance.OverrideMusic(audioClip);
        }

        public static void StopOverrideBackgroundMusic()
        {
            if (!DoesBackgroundMusicExist()) { return; }
            _backgroundMusicInstance.StopOverrideMusic();
        }
        #endregion
        
        #region InstanceMethods
        private void Awake()
        {
            bool wasSettingInitialized = false;
            if (!PlayerPrefsController.MasterVolumeKeyExists()) { PlayerPrefsController.SetMasterVolume(defaultMasterVolume); wasSettingInitialized = true; }
            if (!PlayerPrefsController.BackgroundVolumeKeyExists()) { PlayerPrefsController.SetBackgroundVolume(defaultBackgroundMusicVolume); wasSettingInitialized = true; }
            if (!PlayerPrefsController.SoundEffectsVolumeKeyExists()) { PlayerPrefsController.SetSoundEffectsVolume(defaultSoundEffectsVolume); wasSettingInitialized = true; }
            if (wasSettingInitialized) { PlayerPrefsController.SaveToDisk(); }
        }

        private void Start()
        {
            RefreshVolume();
        }

        private void RefreshVolume()
        {
            if (PlayerPrefsController.MasterVolumeKeyExists())
            {
                volume = Mathf.Clamp(PlayerPrefsController.GetMasterVolume(), _minVolume, 1f);
            }
            masterAudioMixer.audioMixer.SetFloat(_mixerVolumeReference, Mathf.Log10(volume) * 20);
        }
        #endregion
    }
}
