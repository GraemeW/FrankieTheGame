using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace LowDefMustard.Localization
{
    public static class LocalizationLocale
    {
        // State
        // ReSharper disable once StaticMemberInGenericType
        private static bool _isLocaleInitialized;

        #region RuntimeCompliant
        public static string GetCurrentLocaleCode() => LocalizationSettings.SelectedLocale.Identifier.Code;
        public static IEnumerable<string> GetSupportedLocaleCodes() => LocalizationSettings.AvailableLocales.Locales.Select(locale => locale.Identifier.Code);

        public static void SetLocale(string localeCode)
        {
            Locale locale = LocalizationSettings.AvailableLocales.GetLocale(localeCode);
            if (locale == null)
            {
                string fallbackLocaleCode = LocalizationSettings.ProjectLocale.Identifier.Code;
                
                Debug.LogWarning($"No locale found for {localeCode} - setting to fallback {fallbackLocaleCode}"); 
                locale = LocalizationSettings.AvailableLocales.GetLocale(fallbackLocaleCode);
                
                if (locale == null) { Debug.LogWarning($"No locale found for {fallbackLocaleCode}"); return; }
            }

            LocalizationSettings.SelectedLocale = locale;
        }
        #endregion

        // Forces localization async initialization to complete synchronously
        //  - switches the active preview locale to the registered default
        //  - required for editor workflows (e.g. previewing in a consistent language while authoring)
        public static void InitializeDefaultLocale(bool forceInitialization = false)
        {
#if UNITY_EDITOR
            if (_isLocaleInitialized && !forceInitialization) { return; }
            
            TriggerLocalizationSettingsInitialization();
            
            string defaultLocaleCode = LocalizationSettings.ProjectLocale.Identifier.Code;
            SetLocale(defaultLocaleCode);
            _isLocaleInitialized = true;
#endif
        }
        
        public static void TriggerLocalizationSettingsInitialization()
        {
#if UNITY_EDITOR
            LocalizationSettings.InitializationOperation.WaitForCompletion();
#endif
        }
    }
}
