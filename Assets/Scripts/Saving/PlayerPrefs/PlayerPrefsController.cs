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
        private const string _currentSaveKey = "currentSave";
        private const string _currentSaveLeaderStem = "leader";
        private const string _currentSaveLevelStem = "level";
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
        private const string _favouriteFoodStem = "favouriteFood";
        private const string _favouriteThingStem = "favouriteThing";
        private const string _frameFlavourColourStem = "frameFlavourColour";

        // Parameters
        private const float _audioMappingCurveFactor = 2.0f;
        private static readonly float _audioMappingDenominator = Mathf.Exp(_audioMappingCurveFactor) - 1.0f;

        // Events
        public static event Action<Color> frameFlavourUpdated;
        
        #region KeyEnumeration
        private static List<PrefsKeyInfo> GetAllDefinedKeys(List<string> characterPropertiesNames = null, bool includeCurrentSave = false)
        {
            var fixedKeys = new List<PrefsKeyInfo>
            {
                new(_masterVolumeKey, PrefsValueType.Float),
                new(_backgroundVolumeKey, PrefsValueType.Float),
                new(_soundEffectsVolumeKey, PrefsValueType.Float),
                new(_displayWidthKey, PrefsValueType.Int),
                new(_displayHeightKey, PrefsValueType.Int),
                new(_resolutionInitializedKey, PrefsValueType.Int),
                new(_resolutionFullScreenWindowedKey, PrefsValueType.Int),
                new(_resolutionFSWWidthKey, PrefsValueType.Int),
                new(_resolutionFSWHeightKey, PrefsValueType.Int),
                new(_resolutionWindowedWidthKey, PrefsValueType.Int),
                new(_resolutionWindowedHeightKey, PrefsValueType.Int),
                new(_languageKey, PrefsValueType.String),
            };
            
            // Save-Dependent Keys
            if (includeCurrentSave) { fixedKeys.Add(new( _currentSaveKey, PrefsValueType.Int)); }
            
            if (!CurrentSaveKeyExists()) { return fixedKeys; }
            string saveName = GetCurrentSave();
            string key;
                
            if (TryGetCurrentSaveLeaderKey(out key, saveName)) { fixedKeys.Add(new PrefsKeyInfo(key, PrefsValueType.String)); }
            if (TryGetCurrentSaveLevelKey(out key, saveName)) { fixedKeys.Add(new PrefsKeyInfo(key, PrefsValueType.Int)); }

            if (characterPropertiesNames != null)
            {
                foreach (string characterPropertiesName in characterPropertiesNames)
                {
                    if (TryGetCharacterNameKey(characterPropertiesName, out key, saveName)) { fixedKeys.Add(new PrefsKeyInfo(key, PrefsValueType.String)); }
                }
            }
            
            if (TryGetFavouriteFoodKey(out key,saveName)) { fixedKeys.Add(new PrefsKeyInfo(key, PrefsValueType.String)); }
            if (TryGetFavouriteThingKey(out key, saveName)) { fixedKeys.Add(new PrefsKeyInfo(key, PrefsValueType.String)); }
            if (TryGetFrameFlavourKey(out key, saveName)) { fixedKeys.Add(new PrefsKeyInfo(key, PrefsValueType.String)); }
            
            return fixedKeys;
        }

        private static List<PrefsKeyInfo> GetAllKnownKeys(List<string> characterPropertiesNames = null, bool includeCurrentSave = false) => GetAllDefinedKeys(characterPropertiesNames, includeCurrentSave).Where(info => PlayerPrefs.HasKey(info.key)).ToList();
        public static List<PrefsKeyInfo> GetAvailableKeysToAdd(List<string> characterPropertiesNames = null, bool includeCurrentSave = false) => GetAllDefinedKeys(characterPropertiesNames, includeCurrentSave).Where(info => !PlayerPrefs.HasKey(info.key)).ToList();
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
        public static List<PrefsEntryData> GetPrefsEntries(List<string> characterPropertiesNames = null, bool includeCurrentSave = false)
        {
            return GetAllKnownKeys(characterPropertiesNames, includeCurrentSave).Select(info => new PrefsEntryData(info.key, info.type, ReadValue(info.key, info.type))).ToList();
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

        #region Saving
        public static bool CurrentSaveKeyExists() => PlayerPrefs.HasKey(_currentSaveKey);
        public static string GetCurrentSave() => PlayerPrefs.GetString(_currentSaveKey);
        public static void SetCurrentSave(string saveName) => PlayerPrefs.SetString(_currentSaveKey, saveName);
        
        private static bool TryGetCurrentSaveLeaderKey(out string key, string saveName = null)
        {
            if (saveName == null && CurrentSaveKeyExists()) { saveName = PlayerPrefs.GetString(_currentSaveKey); }
            key = saveName != null ? $"{saveName}_{_currentSaveLeaderStem}" : string.Empty;
            return saveName != null;
        }

        private static bool TryGetCurrentSaveLevelKey(out string key, string saveName = null)
        {
            if (saveName == null && CurrentSaveKeyExists()) { saveName = PlayerPrefs.GetString(_currentSaveKey); }
            key = saveName != null ? $"{saveName}_{_currentSaveLevelStem}" : string.Empty;
            return saveName != null;
        }

        public static bool CurrentSaveLeaderKeyExists(string saveName = null) => TryGetCurrentSaveLeaderKey(out string key, saveName) && PlayerPrefs.HasKey(key);
        public static bool CurrentSaveLevelKeyExists(string saveName = null) => TryGetCurrentSaveLevelKey(out string key, saveName) && PlayerPrefs.HasKey(key);

        public static string GetCurrentSaveLeader(string saveName = null) => TryGetCurrentSaveLeaderKey(out string key, saveName) ? PlayerPrefs.GetString(key) : string.Empty;
        public static int GetCurrentSaveLevel(string saveName = null) => TryGetCurrentSaveLevelKey(out string key, saveName) ? PlayerPrefs.GetInt(key) : 0;

        public static void SetCurrentSaveLeader(string leader, string saveName = null)
        {
            if (TryGetCurrentSaveLeaderKey(out string key, saveName)) { PlayerPrefs.SetString(key, leader); }
        }

        public static void SetCurrentSaveLevel(int level, string saveName = null)
        {
            if (TryGetCurrentSaveLevelKey(out string key, saveName)) { PlayerPrefs.SetInt(key, level); }
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
        private static bool TryGetCharacterNameKey(string characterPropertiesName, out string key, string saveName = null)
        {
            if (saveName == null && CurrentSaveKeyExists()) { saveName = PlayerPrefs.GetString(_currentSaveKey); }
            key = saveName != null ? $"{saveName}_{_characterNameStem}_{characterPropertiesName}" : string.Empty;
            return saveName != null;
        }

        private static bool TryGetFavouriteFoodKey(out string key, string saveName = null)
        {
            if (saveName == null && CurrentSaveKeyExists()) { saveName = PlayerPrefs.GetString(_currentSaveKey); }
            key = saveName != null ? $"{saveName}_{_favouriteFoodStem}" : string.Empty;
            return saveName != null;
        }

        private static bool TryGetFavouriteThingKey(out string key, string saveName = null)
        {
            if (saveName == null && CurrentSaveKeyExists()) { saveName = PlayerPrefs.GetString(_currentSaveKey); }
            key = saveName != null ? $"{saveName}_{_favouriteThingStem}" : string.Empty;
            return saveName != null;
        }

        private static bool TryGetFrameFlavourKey(out string key, string saveName = null)
        {
            if (saveName == null && CurrentSaveKeyExists()) { saveName = PlayerPrefs.GetString(_currentSaveKey); }
            key = saveName != null ? $"{saveName}_{_frameFlavourColourStem}" : string.Empty;
            return saveName != null;
        }
        
        public static bool CharacterNameKeyExists(string characterPropertiesName, string saveName = null) => TryGetCharacterNameKey(characterPropertiesName, out string key, saveName) && PlayerPrefs.HasKey(key);
        public static bool FavouriteFoodKeyExists(string saveName = null) => TryGetFavouriteFoodKey(out string key, saveName) && PlayerPrefs.HasKey(key);
        public static bool FavouriteThingKeyExists(string saveName = null) => TryGetFavouriteThingKey(out string key, saveName) && PlayerPrefs.HasKey(key);
        public static bool FrameFlavourColourKeyExists(string saveName = null) => TryGetFrameFlavourKey(out string key, saveName) && PlayerPrefs.HasKey(key);
        
        public static string GetCharacterName(string characterPropertiesName, string saveName = null) => TryGetCharacterNameKey(characterPropertiesName, out string key, saveName) ? PlayerPrefs.GetString(key) : string.Empty;
        public static string GetFavouriteFood(string saveName = null) => TryGetFavouriteFoodKey(out string key, saveName) ? PlayerPrefs.GetString(key) : string.Empty;
        public static string GetFavouriteThing(string saveName = null) => TryGetFavouriteThingKey(out string key, saveName) ? PlayerPrefs.GetString(key) : string.Empty;
        public static Color GetFrameFlavourColour(string saveName = null)
        {
            if (!TryGetFrameFlavourKey(out string key, saveName)) { return Color.white; }
            return ColorUtility.TryParseHtmlString("#" + PlayerPrefs.GetString(key), out Color parsedColor) ? parsedColor : Color.white;
        }

        public static void SetCharacterName(string characterPropertiesName, string characterName, string saveName = null)
        {
            if (TryGetCharacterNameKey(characterPropertiesName, out string key, saveName)) { PlayerPrefs.SetString(key, characterName); }
        }

        public static void SetFavouriteFood(string favouriteFood, string saveName = null)
        {
            if (TryGetFavouriteFoodKey(out string key, saveName)) { PlayerPrefs.SetString(key, favouriteFood); }
        }

        public static void SetFavouriteThing(string favouriteThing, string saveName = null)
        {
            if (TryGetFavouriteThingKey(out string key, saveName)) { PlayerPrefs.SetString(key, favouriteThing); }
        }
        public static void SetFrameFlavourColour(Color frameFlavourColour, string saveName = null)
        {
            if (!TryGetFrameFlavourKey(out string key, saveName)) { return; }
            string frameFlavourColourHex = ColorUtility.ToHtmlStringRGBA(frameFlavourColour);
            PlayerPrefs.SetString(key, frameFlavourColourHex);
            
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
