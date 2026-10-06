using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using LowDefMustard.UIBox;
using LowDefMustard.Localization;
using Frankie.Rendering;
using Frankie.Saving;
using Frankie.Utils.Localization;

namespace Frankie.Menu.UI
{
    // Full-screen-windowed toggle + windowed resolutions:  resolution changes span frames - owning menu runs/stops them
    [Serializable]
    public sealed class DisplayOptions
    {
        // Tunables
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedResolutionHeader;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedDefaultText;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedFullScreenWindowedText;
        [SerializeField] private int windowedResolutionOptionCount = 3;

        // State
        private ToggleChoiceHandle fullScreenWindowedToggle;
        private ChoiceEntryHandle defaultResolutionChoice;
        private ResolutionSetting defaultResolutionSetting;
        private ResolutionSetting openingResolutionSetting;

        // Cached References
        private Action<IEnumerator> runDisplayCoroutine;

        // Events
        public event Action changed;

        #region StaticMethods
        private static void WriteScreenResolutionToPlayerPrefs()
        {
            PlayerPrefsController.SetResolutionSettings(new ResolutionSetting(Screen.fullScreenMode, Screen.width, Screen.height));
        }

        private static IEnumerator WaitForScreenChange(ResolutionSetting resolutionSetting)
        {
            yield return DisplayResolutions.UpdateScreenResolution(resolutionSetting);
            WriteScreenResolutionToPlayerPrefs();
        }
        #endregion

        #region PublicMethods
        public IEnumerable<EntryHandle> CreateEntries(UIToolkitBoxView view, Action<IEnumerator> setRunDisplayCoroutine, Func<Action, Action> withSelectSound)
        {
            runDisplayCoroutine = setRunDisplayCoroutine;
            openingResolutionSetting = new ResolutionSetting(Screen.fullScreenMode, Screen.width, Screen.height);

            var entries = new List<EntryHandle>();
            bool isFullScreenWindowed = Screen.fullScreenMode == FullScreenMode.FullScreenWindow;
            fullScreenWindowedToggle = new ToggleChoiceHandle(view, localizedFullScreenWindowedText.GetSafeLocalizedString(), isFullScreenWindowed, ConfirmResolutionFullScreenWindowed, typeof(OptionsDisplaySection));
            entries.Add(fullScreenWindowedToggle);

            bool isDefaultEntry = true;
            foreach (ResolutionSetting resolutionSetting in DisplayResolutions.GetBestWindowedResolution(windowedResolutionOptionCount))
            {
                var resolutionChoice = new ChoiceEntryHandle(view, GetResolutionText(resolutionSetting, isDefaultEntry), true, withSelectSound(() => ConfirmResolutionWindowed(resolutionSetting)), null, typeof(OptionsResolutionSection));
                entries.Add(resolutionChoice);
                if (!isDefaultEntry) { continue; }

                defaultResolutionChoice = resolutionChoice;
                defaultResolutionSetting = resolutionSetting;
                isDefaultEntry = false;
            }
            return entries;
        }

        public void ResetText(OptionsMenuModel optionsMenuModel)
        {
            optionsMenuModel.resolutionsHeaderText = localizedResolutionHeader.GetSafeLocalizedString();
            fullScreenWindowedToggle?.SetText(localizedFullScreenWindowedText.GetSafeLocalizedString());
            defaultResolutionChoice?.SetText(GetResolutionText(defaultResolutionSetting, true));
        }

        public IEnumerator Revert() => WaitForScreenChange(openingResolutionSetting);

        public void ForceRevert()
        {
            DisplayResolutions.ForceScreenResolution(openingResolutionSetting);
            WriteScreenResolutionToPlayerPrefs();
        }

        public void Save() => WriteScreenResolutionToPlayerPrefs();

        public IEnumerable<TableEntryReference> GetLocalizationEntries()
        {
            yield return localizedResolutionHeader.TableEntryReference;
            yield return localizedDefaultText.TableEntryReference;
            yield return localizedFullScreenWindowedText.TableEntryReference;
        }
        #endregion

        #region PrivateMethods
        private string GetResolutionText(ResolutionSetting resolutionSetting, bool isDefault)
        {
            string resolutionText = $"{resolutionSetting.width} x {resolutionSetting.height}";
            return isDefault ? $"{localizedDefaultText.GetSafeLocalizedString()}: {resolutionText}" : resolutionText;
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
            runDisplayCoroutine?.Invoke(WaitForScreenChange(resolutionSetting));
            changed?.Invoke();
        }

        private void ConfirmResolutionWindowed(ResolutionSetting resolutionSetting)
        {
            fullScreenWindowedToggle.SetValueWithoutNotify(false);
            runDisplayCoroutine?.Invoke(WaitForScreenChange(resolutionSetting));
            WriteScreenResolutionToPlayerPrefs();
            changed?.Invoke();
        }
        #endregion
    }
}
