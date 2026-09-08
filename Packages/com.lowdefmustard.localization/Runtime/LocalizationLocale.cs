using UnityEngine.Localization.Settings;

namespace LowDefMustard.Localization
{
    public static class LocalizationLocale
    {
        #region SupportedLocalizationTypes
        private const string _englishRef = "en";
        private const string _frenchRef = "fr";
        #endregion

        // State
        private static bool _isLocaleInitialized = false;

        #region RuntimeCompliant
        public static SupportedLocalizationType GetCurrentLocalization()
        {
            string localeCode = LocalizationSettings.SelectedLocale.Identifier.Code;
            return GetLocalizationByCode(localeCode);
        }

        public static SupportedLocalizationType GetLocalizationByCode(string localeCode)
        {
            if (localeCode.Contains(_englishRef)) { return SupportedLocalizationType.English; }
            if (localeCode.Contains(_frenchRef)) { return SupportedLocalizationType.French; }
            return SupportedLocalizationType.English;
        }

        public static string GetLocaleCode(SupportedLocalizationType supportedLocalizationType)
        {
            return supportedLocalizationType switch
            {
                SupportedLocalizationType.English => _englishRef,
                SupportedLocalizationType.French => _frenchRef,
                _ => _englishRef
            };
        }

        public static void SetLocale(SupportedLocalizationType supportedLocalizationType)
        {
            LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.GetLocale(GetLocaleCode(supportedLocalizationType));
        }
        #endregion
        
        public static void InitializeEnglishLocale(bool forceInitialization = false)
        {
#if UNITY_EDITOR
            if (_isLocaleInitialized && !forceInitialization) { return; }

            LocalizationSettings.InitializationOperation.WaitForCompletion();
            LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.GetLocale(_englishRef);
            _isLocaleInitialized = true;
#endif
        }
    }
}
