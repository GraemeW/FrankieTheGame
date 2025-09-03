using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frankie.Settings;
using Frankie.Sound;
using Frankie.Utils.UI;

namespace Frankie.Menu.UI
{
    public class OptionsMenu : UIBox
    {
        // Tunables
        [Header("Hookups")]
        [SerializeField] UIChoiceSlider masterVolumeSlider = null;
        [SerializeField] UIChoiceSlider backgroundVolumeSlider = null;
        [SerializeField] UIChoiceSlider soundEffectsVolumeSlider = null;
        [SerializeField] SoundEffects soundUpdateConfirmEffect = null;
        [SerializeField] UIChoiceToggle fullScreenWindowedToggle = null;

        [Header("Sound Settings")]
        [SerializeField] float defaultMasterVolume = 0.8f;
        [SerializeField] float defaultBackgroundVolume = 0.5f;
        [SerializeField] float defaultSoundEffectsVolume = 0.3f;

        [Header("Resolution Settings")]
        [SerializeField] ResolutionSetting targetDefaultResolution = new ResolutionSetting(FullScreenMode.Windowed, 800, 600);

        // Cached References
        BackgroundMusic backgroundMusic = null;
        EscapeMenu escapeMenu = null;

        // State
        float openingMasterVolume;
        float openingBackgroundVolume;
        float openingSoundEffectsVolume;
        ResolutionSetting openingResolutionSetting;

        #region UnityMethods
        private void Start()
        {
            InitializeSoundEffectsSliders();
            InitializeResolutions();

            backgroundMusic = FindAnyObjectByType<BackgroundMusic>(); // find in Start since persistent object, spawned during Awake
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            SubscribeToEscapeMenu(true);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            SubscribeToEscapeMenu(false);
        }
        #endregion

        #region PublicMethods
        public void Setup(EscapeMenu escapeMenu)
        {
            this.escapeMenu = escapeMenu;
            if (gameObject.activeSelf) { SubscribeToEscapeMenu(true); }
        }

        // Save/Exit & Cancel called via Unity Events -- Button Presses
        public void SaveAndExit()
        {
            WriteScreenResolutionToPlayerPrefs();
            WriteVolumeToPlayerPrefs();
            PlayerPrefsController.SaveToDisk();
            Destroy(gameObject);
        }

        public void Cancel()
        {
            ResetOptions();
            HandleClientExit();
            Destroy(gameObject);
        }
        #endregion

        #region InitializationMethods
        private void SubscribeToEscapeMenu(bool enable)
        {
            if (escapeMenu == null) { return; }
            if (enable)
            {
                escapeMenu.escapeMenuItemSelected += Cancel;
            }
            else
            {
                escapeMenu.escapeMenuItemSelected -= Cancel;
            }
        }

        private void InitializeSoundEffectsSliders()
        {
            if (PlayerPrefsController.MasterVolumeKeyExists()) { masterVolumeSlider.SetSliderValue(PlayerPrefsController.GetMasterVolume()); }
            else { masterVolumeSlider.SetSliderValue(defaultMasterVolume); }
            openingMasterVolume = masterVolumeSlider.GetSliderValue();
            masterVolumeSlider.AddOnValueChangeListener((float _) => { ConfirmSoundVolumes(true); });

            if (PlayerPrefsController.BackgroundVolumeKeyExists()) { backgroundVolumeSlider.SetSliderValue(PlayerPrefsController.GetBackgroundVolume()); }
            else { backgroundVolumeSlider.SetSliderValue(defaultBackgroundVolume); }
            openingBackgroundVolume = backgroundVolumeSlider.GetSliderValue();
            backgroundVolumeSlider.AddOnValueChangeListener((float _) => { ConfirmSoundVolumes(false); });

            if (PlayerPrefsController.SoundEffectsVolumeKeyExists()) { soundEffectsVolumeSlider.SetSliderValue(PlayerPrefsController.GetSoundEffectsVolume()); }
            else { soundEffectsVolumeSlider.SetSliderValue(defaultSoundEffectsVolume); }
            openingSoundEffectsVolume = soundEffectsVolumeSlider.GetSliderValue();
            soundEffectsVolumeSlider.AddOnValueChangeListener((float _) => { ConfirmSoundVolumes(true); });
        }

        private void InitializeResolutions()
        {
            fullScreenWindowedToggle.SetToggleValue(Screen.fullScreenMode == FullScreenMode.FullScreenWindow);
            fullScreenWindowedToggle.AddOnValueChangeListener((bool fullScreenWindowed) => { ConfirmResolutionFullScreenWindowed(fullScreenWindowed); });
            openingResolutionSetting = new ResolutionSetting(Screen.fullScreenMode, Screen.width, Screen.height);

            // TODO:  Spawn the resolution selection options
        }

        private void ResetOptions()
        {
            masterVolumeSlider.SetSliderValue(openingMasterVolume);
            backgroundVolumeSlider.SetSliderValue(openingBackgroundVolume);
            soundEffectsVolumeSlider.SetSliderValue(openingSoundEffectsVolume);

            StartCoroutine(WaitForScreenChange(openingResolutionSetting));
        }
        #endregion

        #region UIListenerMethods
        private void ConfirmSoundVolumes(bool playSoundEffect)
        {
            float calculatedVolume = masterVolumeSlider.GetSliderValue() * backgroundVolumeSlider.GetSliderValue();
            backgroundMusic?.SetVolume(calculatedVolume);

            if (playSoundEffect && soundUpdateConfirmEffect != null)
            {
                WriteVolumeToPlayerPrefs();
                soundUpdateConfirmEffect.PlayClip();
            }
        }

        private void ConfirmResolutionFullScreenWindowed(bool fullScreenWindowed)
        {
            ResolutionSetting resolutionSetting;

            if (fullScreenWindowed)
            {
                WriteScreenResolutionToPlayerPrefs(); // Stash windowed settings
                resolutionSetting = DisplayResolutions.GetFSWResolution();
            }
            else
            {
                resolutionSetting = PlayerPrefsController.GetResolutionSettings(false);
                if (resolutionSetting.width == 0 || resolutionSetting.height == 0)
                {
                    resolutionSetting = DisplayResolutions.GetBestWindowedResolution(targetDefaultResolution, 1)[0];
                }
            }
            StartCoroutine(WaitForScreenChange(resolutionSetting));
        }

        private IEnumerator WaitForScreenChange(ResolutionSetting resolutionSetting)
        {
            UnityEngine.Debug.Log($"Resolution is updating to {resolutionSetting.width} x {resolutionSetting.height} on FSW: {resolutionSetting.fullScreenMode}");

            Screen.fullScreenMode = resolutionSetting.fullScreenMode;
            yield return new WaitForEndOfFrame();

            Screen.SetResolution(resolutionSetting.width, resolutionSetting.height, resolutionSetting.fullScreenMode);
            yield return new WaitForEndOfFrame();

            WriteScreenResolutionToPlayerPrefs();
        }

        private void ConfirmResolutionWindowed(int width, int height)
        {
            // TODO:  Implement
            Screen.SetResolution(width, height, FullScreenMode.Windowed);
            WriteScreenResolutionToPlayerPrefs();
        }

        private void WriteVolumeToPlayerPrefs()
        {
            PlayerPrefsController.SetMasterVolume(masterVolumeSlider.GetSliderValue());
            PlayerPrefsController.SetBackgroundVolume(backgroundVolumeSlider.GetSliderValue());
            PlayerPrefsController.SetSoundEffectsVolume(soundEffectsVolumeSlider.GetSliderValue());
        }

        private void WriteScreenResolutionToPlayerPrefs()
        {
            PlayerPrefsController.SetResolutionSettings(new ResolutionSetting(Screen.fullScreenMode, Screen.width, Screen.height));
        }
        #endregion
    }
}
