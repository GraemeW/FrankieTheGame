using UnityEngine;
using Frankie.Rendering;

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

        // Parameters
        private const float _minVolume = 0f;
        private const float _maxVolume = 1f;

        #region Admin
        public static void ClearPlayerPrefs()
        {
            PlayerPrefs.DeleteAll();
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
        public static float GetBackgroundVolume() => PlayerPrefs.GetFloat(_backgroundVolumeKey);
        public static float GetSoundEffectsVolume() => PlayerPrefs.GetFloat(_soundEffectsVolumeKey);
        
        public static void SetMasterVolume(float volume)
        {
            PlayerPrefs.SetFloat(_masterVolumeKey, Mathf.Clamp(volume, _minVolume, _maxVolume));
        }

        public static void SetBackgroundVolume(float volume)
        {
            PlayerPrefs.SetFloat(_backgroundVolumeKey, Mathf.Clamp(volume, _minVolume, _maxVolume));
        }

        public static void SetSoundEffectsVolume(float volume)
        {
            PlayerPrefs.SetFloat(_soundEffectsVolumeKey, Mathf.Clamp(volume, _minVolume, _maxVolume));
        }
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

        private static void SetResolutionInitialized()
        {
            PlayerPrefs.SetInt(_resolutionInitializedKey, 1);
        }

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
    }
}
