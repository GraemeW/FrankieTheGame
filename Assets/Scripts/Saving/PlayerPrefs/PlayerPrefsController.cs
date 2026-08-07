using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using Frankie.Rendering;

namespace Frankie.Saving
{
    public static class PlayerPrefsController
    {
        // Keys
        // Note:  Update KeyEnumeration below when adding new keys to be able to view/edit them in PlayerPrefsEditor
        private const string _masterVolumeKey = "masterVolume";
        private const string _backgroundVolumeKey = "backgroundVolume";
        private const string _soundEffectsVolumeKey = "soundEffectsVolume";
        private const string _displayWidthKey = "displayWidth";
        private const string _displayHeightKey = "displayHeight";
        private const string _resolutionInitializedKey = "resolutionInitialized";
        private const string _resolutionFullScreenWindowedKey = "resolutionFullScreenWindowed";
        private const string _resolutionFSWWidthKey = "resolutionFSWWidth";
        private const string _resolutionFSWHeightKey = "resolutionFSWHeight";
        private const string _resolutionWindowedWidthKey = "resolutionWindowedWidth";
        private const string _resolutionWindowedHeightKey = "resolutionWindowedHeight";
        private const string _languageKey = "languageCode";
        private const string _characterNameStem = "charName";
        private const string _favouriteFoodKey = "favouriteFood";
        private const string _favouriteThingKey = "favouriteThing";
        private const string _frameFlavourColourKey = "frameFlavourColour";

        // Parameters
        private const float _audioMappingCurveFactor = 2.0f;
        private static readonly float _audioMappingDenominator = Mathf.Exp(_audioMappingCurveFactor) - 1.0f;

        // Events
        public static event Action<Color> frameFlavourUpdated;
        
        #region KeyEnumeration
        private static List<PrefsKeyInfo> GetAllKnownKeys(IEnumerable<string> characterPropertiesNames = null)
        {
            var list = new List<PrefsKeyInfo>();

            AddIfExists(list, _masterVolumeKey, PrefsValueType.Float);
            AddIfExists(list, _backgroundVolumeKey, PrefsValueType.Float);
            AddIfExists(list, _soundEffectsVolumeKey, PrefsValueType.Float);
            AddIfExists(list, _displayWidthKey, PrefsValueType.Int);
            AddIfExists(list, _displayHeightKey, PrefsValueType.Int);
            AddIfExists(list, _resolutionInitializedKey, PrefsValueType.Int);
            AddIfExists(list, _resolutionFullScreenWindowedKey, PrefsValueType.Int);
            AddIfExists(list, _resolutionFSWWidthKey, PrefsValueType.Int);
            AddIfExists(list, _resolutionFSWHeightKey, PrefsValueType.Int);
            AddIfExists(list, _resolutionWindowedWidthKey, PrefsValueType.Int);
            AddIfExists(list, _resolutionWindowedHeightKey, PrefsValueType.Int);
            AddIfExists(list, _languageKey, PrefsValueType.String);
            AddIfExists(list, _favouriteFoodKey, PrefsValueType.String);
            AddIfExists(list, _favouriteThingKey, PrefsValueType.String);
            AddIfExists(list, _frameFlavourColourKey, PrefsValueType.String);

            if (characterPropertiesNames == null) { return list; }
            foreach (string characterPropertiesName in characterPropertiesNames)
            {
                AddIfExists(list, GetCharacterNameKey(characterPropertiesName), PrefsValueType.String);
            }
            return list;
        }

        private static void AddIfExists(List<PrefsKeyInfo> list, string key, PrefsValueType type)
        {
            if (PlayerPrefs.HasKey(key)) list.Add(new PrefsKeyInfo(key, type));
        }
        #endregion
        
        #region Admin
        public static void ClearPlayerPrefs(bool save = true)
        {
            PlayerPrefs.DeleteAll();
            if (save) { PlayerPrefs.Save(); }
        }

        public static void SaveToDisk()
        {
            PlayerPrefs.Save();
        }
        #endregion
        
        #region GenericInterface
        public static List<PrefsEntryData> GetPrefsEntries(IEnumerable<string> characterPropertiesNames = null)
        {
            var results = GetAllKnownKeys(characterPropertiesNames).Select(info => new PrefsEntryData(info.key, info.type, ReadValue(info.key, info.type))).ToList();
            results.Sort((a, b) => string.Compare(a.key, b.key, StringComparison.OrdinalIgnoreCase));
            return results;
        }
        
        private static string ReadValue(string key, PrefsValueType type)
        {
            switch (type)
            {
                case PrefsValueType.Int:
                    return PlayerPrefs.GetInt(key).ToString(CultureInfo.InvariantCulture);
                case PrefsValueType.Float:
                    return PlayerPrefs.GetFloat(key).ToString(CultureInfo.InvariantCulture);
                default:
                    return PlayerPrefs.GetString(key);
            }
        }
        
        public static void SetPref(PrefsEntryData entry)
        {
            switch (entry.type)
            {
                case PrefsValueType.Int:
                    if (entry.TryGetValue(out int intValue)) { PlayerPrefs.SetInt(entry.key, intValue); }
                    break;
                case PrefsValueType.Float:
                    if (entry.TryGetValue(out float floatValue)) { PlayerPrefs.SetFloat(entry.key, floatValue); }
                    break;
                case PrefsValueType.String:
                    if (entry.TryGetValue(out string stringValue)) { PlayerPrefs.SetString(entry.key, stringValue); }
                    break;
            }
        }
        
        public static void DeleteKey(string key)
        {
            if (!string.IsNullOrEmpty(key) && PlayerPrefs.HasKey(key)) { PlayerPrefs.DeleteKey(key); }
        }
        #endregion

        #region VolumeSettings
        public static bool MasterVolumeKeyExists() => PlayerPrefs.HasKey(_masterVolumeKey);
        public static bool BackgroundVolumeKeyExists() => PlayerPrefs.HasKey(_backgroundVolumeKey);
        public static bool SoundEffectsVolumeKeyExists() => PlayerPrefs.HasKey(_soundEffectsVolumeKey);
        
        public static float GetMasterVolume() => PlayerPrefs.GetFloat(_masterVolumeKey);
        public static float GetMasterUIVolume() => UnmapVolumeToUI(GetMasterVolume());
        public static float GetBackgroundVolume() => PlayerPrefs.GetFloat(_backgroundVolumeKey);
        public static float GetBackgroundUIVolume() => UnmapVolumeToUI(GetBackgroundVolume());
        public static float GetSoundEffectsVolume() => PlayerPrefs.GetFloat(_soundEffectsVolumeKey);
        public static float GetSoundEffectsUIVolume() => UnmapVolumeToUI(GetSoundEffectsVolume());

        public static void SetMasterVolume(float uiVolume) => PlayerPrefs.SetFloat(_masterVolumeKey, MapUIToVolume(uiVolume));
        public static void SetBackgroundVolume(float uiVolume) => PlayerPrefs.SetFloat(_backgroundVolumeKey, MapUIToVolume(uiVolume));
        public static void SetSoundEffectsVolume(float uiVolume) => PlayerPrefs.SetFloat(_soundEffectsVolumeKey, MapUIToVolume(uiVolume));
        #endregion

        #region DisplaySettings
        public static bool ResolutionInitializedKeyExists() => PlayerPrefs.HasKey(_resolutionInitializedKey);
        public static bool ResolutionFullScreenWindowedKeyExists() => PlayerPrefs.HasKey(_resolutionFullScreenWindowedKey);
        private static bool ResolutionFSWWidthKeyExists() => PlayerPrefs.HasKey(_resolutionFSWWidthKey);
        private static bool ResolutionFSWHeightKeyExists() => PlayerPrefs.HasKey(_resolutionFSWHeightKey);
        private static bool ResolutionWindowedWidthKeyExists() => PlayerPrefs.HasKey(_resolutionWindowedWidthKey);
        private static bool ResolutionWindowedHeightKeyExists() => PlayerPrefs.HasKey(_resolutionWindowedHeightKey);
        public static bool GetResolutionFullScreenWindowed() => PlayerPrefs.GetInt(_resolutionFullScreenWindowedKey) == 1;
        private static void SetCurrentDisplay()
        {
            // Note:  This sets the physical dimensions of the current display
            DisplayInfo displayInfo = Screen.mainWindowDisplayInfo;
            PlayerPrefs.SetInt(_displayWidthKey, displayInfo.width);
            PlayerPrefs.SetInt(_displayHeightKey, displayInfo.height);
        }

        private static void SetResolutionInitialized() => PlayerPrefs.SetInt(_resolutionInitializedKey, 1);

        public static void SetResolutionSettings(ResolutionSetting resolutionSetting)
        {
            bool fullScreenWindowed = resolutionSetting.fullScreenMode == FullScreenMode.FullScreenWindow;

            PlayerPrefs.SetInt(_resolutionFullScreenWindowedKey, fullScreenWindowed ? 1 : 0);
            if (fullScreenWindowed)
            {
                PlayerPrefs.SetInt(_resolutionFSWWidthKey, resolutionSetting.width);
                PlayerPrefs.SetInt(_resolutionFSWHeightKey, resolutionSetting.height);
            }
            else
            {
                PlayerPrefs.SetInt(_resolutionWindowedWidthKey, resolutionSetting.width);
                PlayerPrefs.SetInt(_resolutionWindowedHeightKey, resolutionSetting.height);
            }

            SetCurrentDisplay();
            if (!ResolutionInitializedKeyExists()) { SetResolutionInitialized(); }
        }

        public static ResolutionSetting GetResolutionSettings(bool fullScreenWindowed)
        {
            FullScreenMode fullScreenMode;
            int width = 0;
            int height = 0;

            if (fullScreenWindowed)
            {
                fullScreenMode = FullScreenMode.FullScreenWindow;

                // Expect if FSW key is true others set true as well
                // Safety here in case PlayerPrefs corrupted or otherwise modified to a bad state
                if (ResolutionFSWWidthKeyExists() && ResolutionFSWHeightKeyExists())
                {
                    width = PlayerPrefs.GetInt(_resolutionFSWWidthKey);
                    height = PlayerPrefs.GetInt(_resolutionFSWHeightKey);
                }
            }
            else
            {
                fullScreenMode = FullScreenMode.Windowed;

                if (ResolutionWindowedWidthKeyExists() && ResolutionWindowedHeightKeyExists())
                {
                    width = PlayerPrefs.GetInt(_resolutionWindowedWidthKey);
                    height = PlayerPrefs.GetInt(_resolutionWindowedHeightKey);
                }
            }

            return new ResolutionSetting(fullScreenMode, width, height);
        }
        
        public static bool HasCurrentDisplayChanged()
        {
            if (!PlayerPrefs.HasKey(_displayWidthKey) || !PlayerPrefs.HasKey(_displayHeightKey)) { return true; }

            DisplayInfo currentDisplay = Screen.mainWindowDisplayInfo;
            return currentDisplay.width != PlayerPrefs.GetInt(_displayWidthKey) || currentDisplay.height != PlayerPrefs.GetInt(_displayHeightKey);
        }
        #endregion
        
        #region LanguageSettings
        public static bool LanguageKeyExists() => PlayerPrefs.HasKey(_languageKey);
        public static string GetLanguageCode() => PlayerPrefs.GetString(_languageKey);
        public static void SetLanguageCode(string languageCode) => PlayerPrefs.SetString(_languageKey, languageCode);
        #endregion
        
        #region NameScreenSettings
        private static string GetCharacterNameKey(string characterPropertiesName) => $"{_characterNameStem}_{characterPropertiesName}";
        public static bool CharacterNameKeyExists(string characterPropertiesName) => PlayerPrefs.HasKey(GetCharacterNameKey(characterPropertiesName));
        public static bool FavouriteFoodKeyExists() => PlayerPrefs.HasKey(_favouriteFoodKey);
        public static bool FavouriteThingKeyExists() => PlayerPrefs.HasKey(_favouriteThingKey);
        public static bool FrameFlavourColourKeyExists() => PlayerPrefs.HasKey(_frameFlavourColourKey);
        public static string GetCharacterName(string characterPropertiesName) => PlayerPrefs.GetString(GetCharacterNameKey(characterPropertiesName));
        public static string GetFavouriteFood() => PlayerPrefs.GetString(_favouriteFoodKey);
        public static string GetFavouriteThing() => PlayerPrefs.GetString(_favouriteThingKey);
        public static Color GetFrameFlavourColour()
        {
            string frameFlavourColourHex = PlayerPrefs.GetString(_frameFlavourColourKey);
            return ColorUtility.TryParseHtmlString("#" + frameFlavourColourHex, out Color parsedColor) ? parsedColor : Color.white;
        }
        public static void SetCharacterName(string characterPropertiesName, string characterName) => PlayerPrefs.SetString(GetCharacterNameKey(characterPropertiesName), characterName);
        public static void SetFavouriteFood(string favouriteFood) => PlayerPrefs.SetString(_favouriteFoodKey, favouriteFood);
        public static void SetFavouriteThing(string favouriteThing) => PlayerPrefs.SetString(_favouriteThingKey, favouriteThing);
        public static void SetFrameFlavourColour(Color frameFlavourColour)
        {
            string frameFlavourColourHex = ColorUtility.ToHtmlStringRGBA(frameFlavourColour);
            PlayerPrefs.SetString(_frameFlavourColourKey, frameFlavourColourHex);
            frameFlavourUpdated?.Invoke(frameFlavourColour);
        }
        #endregion
        
        #region HelperMethods
        private static float MapUIToVolume(float uiVolume)
        {
            uiVolume = Mathf.Clamp01(uiVolume);
            return (Mathf.Exp(_audioMappingCurveFactor * uiVolume) - 1.0f) / _audioMappingDenominator;
        }

        private static float UnmapVolumeToUI(float volume)
        {
            volume = Mathf.Clamp01(volume);
            return Mathf.Log(volume * _audioMappingDenominator + 1.0f) / _audioMappingCurveFactor;
        }
        #endregion
    }
}
