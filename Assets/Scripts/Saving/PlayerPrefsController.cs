using UnityEngine;
using Frankie.Rendering;
using Frankie.Utils.UI;

namespace Frankie.Saving
{
    public class PlayerPrefsController : MonoBehaviour
    {
        // Keys
        private const string _masterVolumeKey = "masterVolume";
        private const string _backgroundVolumeKey = "backgroundVolume";
        private const string _soundEffectsVolumeKey = "soundEffectsVolume";
        private const string _displayWidth = "displayWidth";
        private const string _displayHeight = "displayHeight";
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
        private const string _frameFlavourColour = "frameFlavourColour";

        // Parameters
        private const float _audioMappingCurveFactor = 2.0f;
        private static readonly float _audioMappingDenominator = Mathf.Exp(_audioMappingCurveFactor) - 1.0f;

        #region Admin
        public static void ClearPlayerPrefs()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
        }

        public static void SaveToDisk()
        {
            PlayerPrefs.Save();
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
            PlayerPrefs.SetInt(_displayWidth, displayInfo.width);
            PlayerPrefs.SetInt(_displayHeight, displayInfo.height);
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
            if (!PlayerPrefs.HasKey(_displayWidth) || !PlayerPrefs.HasKey(_displayHeight)) { return true; }

            DisplayInfo currentDisplay = Screen.mainWindowDisplayInfo;
            return currentDisplay.width != PlayerPrefs.GetInt(_displayWidth) || currentDisplay.height != PlayerPrefs.GetInt(_displayHeight);
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
        public static bool FrameFlavourColourKeyExists() => PlayerPrefs.HasKey(_frameFlavourColour);
        public static string GetCharacterName(string characterPropertiesName) => PlayerPrefs.GetString(GetCharacterNameKey(characterPropertiesName));
        public static string GetFavouriteFood() => PlayerPrefs.GetString(_favouriteFoodKey);
        public static string GetFavouriteThing() => PlayerPrefs.GetString(_favouriteThingKey);
        public static Color GetFrameFlavourColour()
        {
            string frameFlavourColourHex = PlayerPrefs.GetString(_frameFlavourColour);
            return ColorUtility.TryParseHtmlString("#" + frameFlavourColourHex, out Color parsedColor) ? parsedColor : Color.white;
        }
        public static void SetCharacterName(string characterPropertiesName, string characterName) => PlayerPrefs.SetString(GetCharacterNameKey(characterPropertiesName), characterName);
        public static void SetFavouriteFood(string favouriteFood) => PlayerPrefs.SetString(_favouriteFoodKey, favouriteFood);
        public static void SetFavouriteThing(string favouriteThing) => PlayerPrefs.SetString(_favouriteThingKey, favouriteThing);
        public static void SetFrameFlavourColour(Color frameFlavourColour)
        {
            string frameFlavourColourHex = ColorUtility.ToHtmlStringRGBA(frameFlavourColour);
            PlayerPrefs.SetString(_frameFlavourColour, frameFlavourColourHex);
            UIFrame.SetGlobalFrameFlavour(frameFlavourColour);
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
