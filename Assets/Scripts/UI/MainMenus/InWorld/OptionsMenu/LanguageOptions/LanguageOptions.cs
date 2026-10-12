using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using LowDefMustard.UIBox;
using LowDefMustard.Localization;
using Frankie.Saving;
using Frankie.Utils.Localization;

namespace Frankie.Menu.UI
{
    // Language row:  a locale change applies immediately; listeners re-text whatever is on screen
    [Serializable]
    public sealed class LanguageOptions
    {
        // Tunables
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedLanguageSelectText;

        // State
        private string openingLocale;

        // Events
        public event Action localeChanged;

        #region PublicMethods
        public IEnumerable<EntryHandle> CreateEntries(UIToolkitBoxView view, Func<Action, Action> withSelectSound)
        {
            openingLocale = LocalizationLocale.GetCurrentLocaleCode();
            var languageGroup = new ChoiceGroupHandle(view, typeof(OptionsLanguageSection));
            foreach (string localeCode in LocalizationLocale.GetSupportedLocaleCodes())
            {
                languageGroup.AddSubChoice(localeCode, withSelectSound(() => SetLocale(localeCode)));
            }
            return new EntryHandle[] { languageGroup };
        }

        public void ResetText(OptionsMenuModel optionsMenuModel)
        {
            optionsMenuModel.languageHeaderText = localizedLanguageSelectText.GetSafeLocalizedString();
        }

        public void Revert() => SetLocale(openingLocale);

        public IEnumerable<TableEntryReference> GetLocalizationEntries()
        {
            yield return localizedLanguageSelectText.TableEntryReference;
        }
        #endregion

        #region PrivateMethods
        private void SetLocale(string localeCode)
        {
            Debug.Log($"Current locale is {LocalizationLocale.GetCurrentLocaleCode()} - updating to {localeCode}");
            LocalizationLocale.SetLocale(localeCode);
            PlayerPrefsController.SetLanguageCode(localeCode);
            localeChanged?.Invoke();
        }
        #endregion
    }
}
