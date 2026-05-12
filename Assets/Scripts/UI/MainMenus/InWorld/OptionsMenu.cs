using System;
using System.Collections;
using System.Collections.Generic;
using Frankie.Control;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using TMPro;
using Frankie.Sound;
using Frankie.Rendering;
using Frankie.Saving;
using Frankie.Utils.Localization;
using Frankie.Utils.UI;

namespace Frankie.Menu.UI
{
    public class OptionsMenu : UIBox, ILocalizable
    {
        [Header("Text")]
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedOptionsHeader;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedMasterVolumeText;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedBackgroundVolumeText;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedSoundEffectsVolumeText;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedResolutionHeader;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedDefaultText;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedFullScreenWindowedText;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedLanguageSelectText;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedConfirmText;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedCancelText;
        [Header("Hookups")]
        [SerializeField] private TMP_Text optionsHeaderField;
        [SerializeField] private UIChoiceSlider masterVolumeSlider;
        [SerializeField] private UIChoiceSlider backgroundVolumeSlider;
        [SerializeField] private UIChoiceSlider soundEffectsVolumeSlider;
        [SerializeField] private SoundEffects soundUpdateConfirmEffect;
        [SerializeField] private TMP_Text resolutionsHeaderField;
        [SerializeField] private UIChoiceToggle fullScreenWindowedToggle;
        [SerializeField] private Transform resolutionOptionsParent;
        [SerializeField] private TMP_Text languageHeaderField;
        [SerializeField] private Transform languageOptionsParent;
        [SerializeField] private UIChoiceButton confirmOption;
        [SerializeField] private UIChoiceButton cancelOption;
        [Header("Default Sound Settings")]
        [SerializeField] private float defaultMasterVolume = 0.8f;
        [SerializeField] private float defaultBackgroundVolume = 0.5f;
        [SerializeField] private float defaultSoundEffectsVolume = 0.3f;
        [Header("Default Resolution Settings")]
        [SerializeField] private int windowedResolutionOptionCount = 3;
        
        // Cached References
        private BackgroundMusic backgroundMusic;
        private EscapeMenu escapeMenu;

        // State
        private float openingMasterVolume;
        private float openingBackgroundVolume;
        private float openingSoundEffectsVolume;
        private ResolutionSetting openingResolutionSetting;
        private SupportedLocalizationType openingLocalizationType;

        #region StaticMethods
        private static void WriteScreenResolutionToPlayerPrefs()
        {
            PlayerPrefsController.SetResolutionSettings(new ResolutionSetting(Screen.fullScreenMode, Screen.width, Screen.height));
        }
        #endregion
        
        #region UnityMethods
        private void Start()
        {
            backgroundMusic = BackgroundMusic.FindBackgroundMusic(); // find in Start since persistent object, spawned during Awake

            InitializeLocalization();
            
            int choiceIndex = 0;
            InitializeSoundEffectsSliders(ref choiceIndex);
            InitializeResolutions(ref choiceIndex);
            InitializeLanguageSelection(ref choiceIndex);
            if (confirmOption != null)
            {
                confirmOption.SetChoiceOrder(choiceIndex);
                confirmOption.AddOnClickListener(SaveAndExit);
                choiceIndex++;
            }
            if (cancelOption != null)
            {
                cancelOption.SetChoiceOrder(choiceIndex);
                cancelOption.AddOnClickListener(Cancel);
            }
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
        
        #region LocalizationMethods
        public LocalizationTableType localizationTableType { get; } = LocalizationTableType.UI;
        public List<TableEntryReference> GetLocalizationEntries()
        {
            return new List<TableEntryReference>
            {
                localizedOptionsHeader.TableEntryReference,
                localizedMasterVolumeText.TableEntryReference,
                localizedBackgroundVolumeText.TableEntryReference,
                localizedSoundEffectsVolumeText.TableEntryReference,
                localizedResolutionHeader.TableEntryReference,
                localizedFullScreenWindowedText.TableEntryReference,
                localizedLanguageSelectText.TableEntryReference,
                localizedConfirmText.TableEntryReference,
                localizedCancelText.TableEntryReference
            };
        }
        
        private void InitializeLocalization()
        {
            if (optionsHeaderField != null) { optionsHeaderField.SetText(localizedOptionsHeader.GetSafeLocalizedString()); }
            if (masterVolumeSlider != null) { masterVolumeSlider.SetText(localizedMasterVolumeText.GetSafeLocalizedString()); }
            if (backgroundVolumeSlider != null) { backgroundVolumeSlider.SetText(localizedBackgroundVolumeText.GetSafeLocalizedString()); }
            if (soundEffectsVolumeSlider != null) { soundEffectsVolumeSlider.SetText(localizedSoundEffectsVolumeText.GetSafeLocalizedString()); }
            if (resolutionsHeaderField != null) { resolutionsHeaderField.SetText(localizedResolutionHeader.GetSafeLocalizedString()); }
            if (fullScreenWindowedToggle != null) { fullScreenWindowedToggle.SetText(localizedFullScreenWindowedText.GetSafeLocalizedString()); }
            if (languageHeaderField != null) { languageHeaderField.SetText(localizedLanguageSelectText.GetSafeLocalizedString()); }
            if (confirmOption != null) { confirmOption.SetText(localizedConfirmText.GetSafeLocalizedString()); }
            if (cancelOption != null) { cancelOption.SetText(localizedCancelText.GetSafeLocalizedString()); }
        }
        #endregion

        #region PublicMethods
        public void Setup(EscapeMenu setEscapeMenu)
        {
            escapeMenu = setEscapeMenu;
            if (gameObject.activeSelf) { SubscribeToEscapeMenu(true); }
        }

        private IEnumerator CancelRoutine()
        {
            yield return ResetOptions(); 
            HandleClientExit();
            Destroy(gameObject);
        }
        #endregion

        #region PrivateMethods
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
            masterVolumeSlider.SetSliderValue(PlayerPrefsController.MasterVolumeKeyExists() ? PlayerPrefsController.GetMasterVolume() : defaultMasterVolume);
            openingMasterVolume = masterVolumeSlider.GetSliderValue();
            masterVolumeSlider.AddOnValueChangeListener(delegate { ConfirmSoundVolumes(true); });
            masterVolumeSlider.SetChoiceOrder(choiceIndex);
            choiceIndex++;

            backgroundVolumeSlider.SetSliderValue(PlayerPrefsController.BackgroundVolumeKeyExists() ? PlayerPrefsController.GetBackgroundVolume() : defaultBackgroundVolume);
            openingBackgroundVolume = backgroundVolumeSlider.GetSliderValue();
            backgroundVolumeSlider.AddOnValueChangeListener(delegate { ConfirmSoundVolumes(false); });
            backgroundVolumeSlider.SetChoiceOrder(choiceIndex);
            choiceIndex++;

            soundEffectsVolumeSlider.SetSliderValue(PlayerPrefsController.SoundEffectsVolumeKeyExists() ? PlayerPrefsController.GetSoundEffectsVolume() : defaultSoundEffectsVolume);
            openingSoundEffectsVolume = soundEffectsVolumeSlider.GetSliderValue();
            soundEffectsVolumeSlider.AddOnValueChangeListener(delegate { ConfirmSoundVolumes(true); });
            soundEffectsVolumeSlider.SetChoiceOrder(choiceIndex);
            choiceIndex++;
        }

        private void InitializeResolutions(ref int choiceIndex)
        {
            openingResolutionSetting = new ResolutionSetting(Screen.fullScreenMode, Screen.width, Screen.height);

            fullScreenWindowedToggle.SetToggleValue(Screen.fullScreenMode == FullScreenMode.FullScreenWindow);
            fullScreenWindowedToggle.AddOnValueChangeListener( ConfirmResolutionFullScreenWindowed);
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
                        resolutionChoiceButton.SetText($"{localizedDefaultText.GetSafeLocalizedString()}: {resolutionSetting.width} x {resolutionSetting.height}");
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

        private void InitializeLanguageSelection(ref int choiceIndex)
        {
            openingLocalizationType = LocalizationTool.GetCurrentLocalization();
            
            foreach (SupportedLocalizationType supportedLocalizationType in Enum.GetValues(typeof(SupportedLocalizationType)))
            {
                GameObject languageOption = Instantiate(optionButtonPrefab, languageOptionsParent);
                if (languageOption.TryGetComponent(out UIChoiceButton languageChoiceButton))
                {
                    languageChoiceButton.SetText(LocalizationTool.GetLocaleCode(supportedLocalizationType));
                    languageChoiceButton.AddOnClickListener(delegate { ConfirmLocalizationChange(supportedLocalizationType); });
                    languageChoiceButton.SetChoiceOrder(choiceIndex);
                    choiceIndex++;
                }
                else { Destroy(languageOption); } // incorrect input type
            }
        }

        private IEnumerator ResetOptions()
        {
            ResetNonDelayOptions();
            yield return WaitForScreenChange(openingResolutionSetting);
        }

        private void ForceResetOptions()
        {
            ResetNonDelayOptions();
            ForceScreenChange(openingResolutionSetting);
        }

        private void ResetNonDelayOptions()
        {
            masterVolumeSlider.SetSliderValue(openingMasterVolume);
            backgroundVolumeSlider.SetSliderValue(openingBackgroundVolume);
            soundEffectsVolumeSlider.SetSliderValue(openingSoundEffectsVolume);
            ConfirmLocalizationChange(openingLocalizationType);
        }
        
        private void SaveAndExit()
        {
            WriteScreenResolutionToPlayerPrefs();
            WriteVolumeToPlayerPrefs();
            PlayerPrefsController.SaveToDisk();
            Destroy(gameObject);
        }

        private void Cancel()
        {
            // Since resolution updates done over several frames, need to kick off Coroutine
            StartCoroutine(CancelRoutine());
        }
        #endregion

        #region UIListenerMethods
        private void ConfirmSoundVolumes(bool playSoundEffect)
        {
            float calculatedVolume = masterVolumeSlider.GetSliderValue() * backgroundVolumeSlider.GetSliderValue();
            backgroundMusic?.SetVolume(calculatedVolume);

            if (!playSoundEffect || soundUpdateConfirmEffect == null) return;
            WriteVolumeToPlayerPrefs();
            soundUpdateConfirmEffect.PlayClip();
        }

        private void ConfirmResolutionFullScreenWindowed(bool fullScreenWindowed)
        {
            ResolutionSetting resolutionSetting;

            if (fullScreenWindowed)
            {
                WriteScreenResolutionToPlayerPrefs(); // Stash windowed settings
                resolutionSetting = DisplayResolutions.GetFullScreenWidthResolution();
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

        private void ForceScreenChange(ResolutionSetting resolutionSetting)
        {
            DisplayResolutions.ForceScreenResolution(resolutionSetting);
            WriteScreenResolutionToPlayerPrefs();
        }

        private void ConfirmResolutionWindowed(ResolutionSetting resolutionSetting)
        {
            fullScreenWindowedToggle.SetToggleValueSilently(false);
            StartCoroutine(WaitForScreenChange(resolutionSetting));
            WriteScreenResolutionToPlayerPrefs();
        }

        private void WriteLocalizationToPlayerPrefs(SupportedLocalizationType supportedLocalizationType)
        {
            string localeCode = LocalizationTool.GetLocaleCode(supportedLocalizationType);
            PlayerPrefsController.SetLanguageCode(localeCode);
        }
        
        private void ConfirmLocalizationChange(SupportedLocalizationType supportedLocalizationType)
        {
            Debug.Log($"Current locale is {LocalizationTool.GetLocaleCode(LocalizationTool.GetCurrentLocalization())} - updating to {LocalizationTool.GetLocaleCode(supportedLocalizationType)}");
            
            LocalizationTool.SetLocale(supportedLocalizationType);
            InitializeLocalization();
            WriteLocalizationToPlayerPrefs(supportedLocalizationType);
        }

        private void WriteVolumeToPlayerPrefs()
        {
            PlayerPrefsController.SetMasterVolume(masterVolumeSlider.GetSliderValue());
            PlayerPrefsController.SetBackgroundVolume(backgroundVolumeSlider.GetSliderValue());
            PlayerPrefsController.SetSoundEffectsVolume(soundEffectsVolumeSlider.GetSliderValue());
        }
        #endregion
        
        #region InputHandling
        public override bool HandleGlobalInput(PlayerInputType playerInputType)
        {
            if (playerInputType is PlayerInputType.Cancel or PlayerInputType.Option or PlayerInputType.Escape)
            {
                ForceResetOptions();
            }
            return StandardHandleGlobalInput(playerInputType);
        }
        #endregion
    }
}
