using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization.Settings;

namespace LowDefMustard.Localization
{
    // TLocaleType is the caller's locale enum (e.g. a project-specific SupportedLocalizationType)
    // Project implementation should define its own empty alias (e.g. LocalizationLocale : LocalizationLocaleBase<SupportedLocalizationType>)
    
    public abstract class LocalizationLocaleBase<TLocaleType> : LocalizationLocaleCore where TLocaleType : struct, Enum
    {
        // State
        private static Dictionary<TLocaleType, string> _localeCodes;
        private static TLocaleType? _defaultLocale;
        // ReSharper disable once StaticMemberInGenericType
        private static bool _isLocaleInitialized;

        #region Configuration
        public static void RegisterLocaleCodes(IReadOnlyDictionary<TLocaleType, string> localeCodes, TLocaleType defaultLocale)
        {
            _localeCodes ??= new Dictionary<TLocaleType, string>();
            foreach (KeyValuePair<TLocaleType, string> entry in localeCodes) { _localeCodes[entry.Key] = entry.Value; }
            _defaultLocale = defaultLocale;
        }
        #endregion

        #region RuntimeCompliant
        public static TLocaleType GetCurrentLocalization()
        {
            string localeCode = LocalizationSettings.SelectedLocale.Identifier.Code;
            return GetLocalizationByCode(localeCode);
        }

        public static TLocaleType GetLocalizationByCode(string localeCode)
        {
            if (_localeCodes == null) { return GetFallbackDefaultLocale(); }
            
            foreach (KeyValuePair<TLocaleType, string> entry in _localeCodes) { if (localeCode.Contains(entry.Value)) { return entry.Key; } }
            return GetFallbackDefaultLocale();
        }

        public static string GetLocaleCode(TLocaleType localeType)
        {
            if (_localeCodes != null && _localeCodes.TryGetValue(localeType, out string localeCode)) { return localeCode; }

            Debug.LogWarning($"No locale code registered for '{typeof(TLocaleType).Name}.{localeType}' - falling back to the default locale");
            return _localeCodes != null && _defaultLocale != null && _localeCodes.TryGetValue(_defaultLocale.Value, out string defaultLocaleCode) ? defaultLocaleCode : fallbackLocaleCode;
        }

        public static void SetLocale(TLocaleType localeType)
        {
            LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.GetLocale(GetLocaleCode(localeType));
        }
        #endregion

        // Forces localization async initialization to complete synchronously
        //  - switches the active preview locale to the registered default
        //  - required for editor workflows (e.g. previewing in a consistent language while authoring)
        public static void InitializeDefaultLocale(bool forceInitialization = false)
        {
#if UNITY_EDITOR
            if (_isLocaleInitialized && !forceInitialization) { return; }
            if (_defaultLocale == null)
            {
                Debug.LogWarning($"No default locale registered for '{typeof(TLocaleType).Name}'");
                return;
            }

            TriggerLocalizationSettingsInitialization();
            LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.GetLocale(GetLocaleCode(_defaultLocale.Value));
            _isLocaleInitialized = true;
#endif
        }

        private static TLocaleType GetFallbackDefaultLocale()
        {
            if (_defaultLocale != null) { return _defaultLocale.Value; }

            Debug.LogWarning($"No default locale registered for '{typeof(TLocaleType).Name}' - falling back to '{default(TLocaleType)}'");
            return default;
        }
    }

    public abstract class LocalizationLocaleCore
    {
        // Const
        protected const string fallbackLocaleCode = "en";

        public static void TriggerLocalizationSettingsInitialization()
        {
#if UNITY_EDITOR
            LocalizationSettings.InitializationOperation.WaitForCompletion();
#endif
        }
    }
}
