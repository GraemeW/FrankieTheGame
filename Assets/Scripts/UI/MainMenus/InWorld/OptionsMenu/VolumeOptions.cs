using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using LowDefMustard.UIBox;
using LowDefMustard.Localization;
using Frankie.Saving;
using Frankie.Sound;
using Frankie.Utils.Localization;

namespace Frankie.Menu.UI
{
    // Volume sliders:  changes apply (and persist to prefs) immediately; Revert restores the values from when the menu opened
    [Serializable]
    public sealed class VolumeOptions
    {
        // Tunables
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedMasterVolumeText;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedBackgroundVolumeText;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedSoundEffectsVolumeText;
        [SerializeField] private SoundEffects soundUpdateConfirmEffect;
        [SerializeField] private float defaultMasterVolume = 0.8f;
        [SerializeField] private float defaultBackgroundVolume = 0.5f;
        [SerializeField] private float defaultSoundEffectsVolume = 0.3f;
        [SerializeField] private float volumeStep = 0.1f;

        // State
        private SliderChoiceHandle masterVolumeSlider;
        private SliderChoiceHandle backgroundVolumeSlider;
        private SliderChoiceHandle soundEffectsVolumeSlider;
        private float openingMasterVolume;
        private float openingBackgroundVolume;
        private float openingSoundEffectsVolume;

        // Events
        public event Action changed;

        #region PublicMethods
        public IEnumerable<EntryHandle> CreateEntries(UIToolkitBoxView view)
        {
            float masterVolume = PlayerPrefsController.MasterVolumeKeyExists() ? PlayerPrefsController.GetMasterUIVolume() : defaultMasterVolume;
            masterVolumeSlider = CreateSlider(view, localizedMasterVolumeText, masterVolume, true);
            openingMasterVolume = masterVolumeSlider.value;

            float backgroundVolume = PlayerPrefsController.BackgroundVolumeKeyExists() ? PlayerPrefsController.GetBackgroundUIVolume() : defaultBackgroundVolume;
            backgroundVolumeSlider = CreateSlider(view, localizedBackgroundVolumeText, backgroundVolume, false);
            openingBackgroundVolume = backgroundVolumeSlider.value;

            float soundEffectsVolume = PlayerPrefsController.SoundEffectsVolumeKeyExists() ? PlayerPrefsController.GetSoundEffectsUIVolume() : defaultSoundEffectsVolume;
            soundEffectsVolumeSlider = CreateSlider(view, localizedSoundEffectsVolumeText, soundEffectsVolume, true);
            openingSoundEffectsVolume = soundEffectsVolumeSlider.value;

            return new EntryHandle[] { masterVolumeSlider, backgroundVolumeSlider, soundEffectsVolumeSlider };
        }

        public void ResetText()
        {
            masterVolumeSlider?.SetText(localizedMasterVolumeText.GetSafeLocalizedString());
            backgroundVolumeSlider?.SetText(localizedBackgroundVolumeText.GetSafeLocalizedString());
            soundEffectsVolumeSlider?.SetText(localizedSoundEffectsVolumeText.GetSafeLocalizedString());
        }

        public void Revert()
        {
            masterVolumeSlider.SetValue(openingMasterVolume);
            backgroundVolumeSlider.SetValue(openingBackgroundVolume);
            soundEffectsVolumeSlider.SetValue(openingSoundEffectsVolume);
        }

        public void Save() => WriteVolumeToPlayerPrefs();

        public IEnumerable<TableEntryReference> GetLocalizationEntries()
        {
            yield return localizedMasterVolumeText.TableEntryReference;
            yield return localizedBackgroundVolumeText.TableEntryReference;
            yield return localizedSoundEffectsVolumeText.TableEntryReference;
        }
        #endregion

        #region PrivateMethods
        private SliderChoiceHandle CreateSlider(UIToolkitBoxView view, LocalizedString localizedText, float volume, bool playSoundEffect)
        {
            return new SliderChoiceHandle(view, localizedText.GetSafeLocalizedString(), volume, volumeStep, _ => ConfirmSoundVolumes(playSoundEffect), typeof(OptionsVolumeSection));
        }

        private void ConfirmSoundVolumes(bool playSoundEffect)
        {
            WriteVolumeToPlayerPrefs();
            CoreAudio.RefreshMasterVolume();
            CoreAudio.RefreshBackgroundMusicVolume();
            if (playSoundEffect && soundUpdateConfirmEffect != null) { soundUpdateConfirmEffect.PlayClip(); }
            changed?.Invoke();
        }

        private void WriteVolumeToPlayerPrefs()
        {
            PlayerPrefsController.SetMasterVolume(masterVolumeSlider.value);
            PlayerPrefsController.SetBackgroundVolume(backgroundVolumeSlider.value);
            PlayerPrefsController.SetSoundEffectsVolume(soundEffectsVolumeSlider.value);
        }
        #endregion
    }
}
