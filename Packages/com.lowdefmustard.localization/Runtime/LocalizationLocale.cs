using System.Collections.Generic;
using System.Linq;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using LowDefMustard.Core;

namespace LowDefMustard.Localization
{
    public static partial class LocalizationLocale
    {
        // State
        [AutoStaticsCleanup] private static bool _isLocaleInitialized;

        #region RuntimeCompliant
        public static string GetCurrentLocaleCode() => LocalizationSettings.SelectedLocale.Identifier.Code;
        public static IEnumerable<string> GetSupportedLocaleCodes() => LocalizationSettings.AvailableLocales.Locales.Select(locale => locale.Identifier.Code);

        public static void SetLocale(string localeCode)
        {
            Locale locale = LocalizationSettings.AvailableLocales.GetLocale(localeCode);
            if (locale == null)
            {
                string fallbackLocaleCode = LocalizationSettings.ProjectLocale.Identifier.Code;
                
                LDMLog.Warning(LDMLogCategory.Localization, $"No locale found for code '{localeCode}'. Falling back to '{fallbackLocaleCode}'.");
                locale = LocalizationSettings.AvailableLocales.GetLocale(fallbackLocaleCode);
                
                if (locale == null) { LDMLog.Error(LDMLogCategory.Localization, $"No locale found for the fallback code '{fallbackLocaleCode}'. Leaving the locale unchanged."); return; }
            }

            LocalizationSettings.SelectedLocale = locale;
            LDMLog.Important(LDMLogCategory.Localization, $"Set locale to '{locale.Identifier.Code}'.");
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
