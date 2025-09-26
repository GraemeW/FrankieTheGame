using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frankie.Sound;
using Frankie.Rendering;
using Frankie.Saving;
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
        [SerializeField] Transform resolutionOptionsParent = null;
        [SerializeField] UIChoice confirmOption = null;
        [SerializeField] UIChoice cancelOption = null;

        [Header("Sound Settings")]
        [SerializeField] float defaultMasterVolume = 0.8f;
        [SerializeField] float defaultBackgroundVolume = 0.5f;
        [SerializeField] float defaultSoundEffectsVolume = 0.3f;

        [Header("Resolution Settings")]
        [SerializeField] int windowedResolutionOptionCount = 3;

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
            backgroundMusic = FindAnyObjectByType<BackgroundMusic>(); // find in Start since persistent object, spawned during Awake

            int choiceIndex = 0;
            InitializeSoundEffectsSliders(ref choiceIndex);
            InitializeResolutions(ref choiceIndex);
            confirmOption?.SetChoiceOrder(choiceIndex);
            choiceIndex++;
            cancelOption?.SetChoiceOrder(choiceIndex);
            SetUpChoiceOptions();
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
            // Since resolution updates done over several frames, need to kick off Coroutine
            StartCoroutine(CancelRoutine());
        }

        private IEnumerator CancelRoutine()
        {
            yield return ResetOptions(); 
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

        private void InitializeSoundEffectsSliders(ref int choiceIndex)
        {
            if (PlayerPrefsController.MasterVolumeKeyExists()) { masterVolumeSlider.SetSliderValue(PlayerPrefsController.GetMasterVolume()); }
            else { masterVolumeSlider.SetSliderValue(defaultMasterVolume); }
            openingMasterVolume = masterVolumeSlider.GetSliderValue();
            masterVolumeSlider.AddOnValueChangeListener(delegate { ConfirmSoundVolumes(true); });
            masterVolumeSlider.SetChoiceOrder(choiceIndex);
            choiceIndex++;

            if (PlayerPrefsController.BackgroundVolumeKeyExists()) { backgroundVolumeSlider.SetSliderValue(PlayerPrefsController.GetBackgroundVolume()); }
            else { backgroundVolumeSlider.SetSliderValue(defaultBackgroundVolume); }
            openingBackgroundVolume = backgroundVolumeSlider.GetSliderValue();
            backgroundVolumeSlider.AddOnValueChangeListener(delegate { ConfirmSoundVolumes(false); });
            backgroundVolumeSlider.SetChoiceOrder(choiceIndex);
            choiceIndex++;

            if (PlayerPrefsController.SoundEffectsVolumeKeyExists()) { soundEffectsVolumeSlider.SetSliderValue(PlayerPrefsController.GetSoundEffectsVolume()); }
            else { soundEffectsVolumeSlider.SetSliderValue(defaultSoundEffectsVolume); }
            openingSoundEffectsVolume = soundEffectsVolumeSlider.GetSliderValue();
            soundEffectsVolumeSlider.AddOnValueChangeListener(delegate { ConfirmSoundVolumes(true); });
            soundEffectsVolumeSlider.SetChoiceOrder(choiceIndex);
            choiceIndex++;
        }

        private void InitializeResolutions(ref int choiceIndex)
        {
            openingResolutionSetting = new ResolutionSetting(Screen.fullScreenMode, Screen.width, Screen.height);

            fullScreenWindowedToggle.SetToggleValue(Screen.fullScreenMode == FullScreenMode.FullScreenWindow);
            fullScreenWindowedToggle.AddOnValueChangeListener( delegate(bool fullScreenWindowed) { ConfirmResolutionFullScreenWindowed(fullScreenWindowed); });
            fullScreenWindowedToggle.SetChoiceOrder(choiceIndex);
            choiceIndex++;

            bool defaultEntry = true;
            foreach (ResolutionSetting resolutionSetting in DisplayResolutions.GetBestWindowedResolution(windowedResolutionOptionCount))
            {
                GameObject resolutionOption = Instantiate(optionButtonPrefab, resolutionOptionsParent);
                if (resolutionOption.TryGetComponent(out UIChoiceButton resolutionChoiceButton))
                {
                    if (defaultEntry)
                    {
                        resolutionChoiceButton.SetText($"Default: {resolutionSetting.width} x {resolutionSetting.height}");
                        defaultEntry = false;
                    }
                    else
                    {
                        resolutionChoiceButton.SetText($"{resolutionSetting.width} x {resolutionSetting.height}");
                    }
                    resolutionChoiceButton.AddOnClickListener(delegate { ConfirmResolutionWindowed(resolutionSetting); });
                    resolutionChoiceButton.SetChoiceOrder(choiceIndex);
                    choiceIndex++;
                }
                else { Destroy(resolutionOption); } // incorrect input type
            }
        }

        private IEnumerator ResetOptions()
        {
            masterVolumeSlider.SetSliderValue(openingMasterVolume);
            backgroundVolumeSlider.SetSliderValue(openingBackgroundVolume);
            soundEffectsVolumeSlider.SetSliderValue(openingSoundEffectsVolume);

            yield return WaitForScreenChange(openingResolutionSetting);
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
                    resolutionSetting = DisplayResolutions.GetBestWindowedResolution(1)[0];
                }
            }
            StartCoroutine(WaitForScreenChange(resolutionSetting));
        }

        private IEnumerator WaitForScreenChange(ResolutionSetting resolutionSetting)
        {
            yield return DisplayResolutions.UpdateScreenResolution(resolutionSetting);
            WriteScreenResolutionToPlayerPrefs();
        }

        private void ConfirmResolutionWindowed(ResolutionSetting resolutionSetting)
        {
            fullScreenWindowedToggle.SetToggleValueSilently(false);
            StartCoroutine(WaitForScreenChange(resolutionSetting));
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
