using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Settings
{
    public class PlayerPrefsController : MonoBehaviour
    {
        // Keys
        const string MASTER_VOLUME_KEY = "masterVolume";
        const string BACKGROUND_VOLUME_KEY = "backgroundVolume";
        const string SOUND_EFFECTS_VOLUME_KEY = "soundEffectsVolume";
        const string RESOLUTION_FULL_SCREEN_WINDOWED_KEY = "resolutionFullScreenWindowed";
        const string RESOLUTION_FSW_WIDTH_KEY = "resolutionFSWWidth";
        const string RESOLUTION_FSW_HEIGHT_KEY = "resolutionFSWHeight";
        const string RESOLUTION_WINDOWED_WIDTH_KEY = "resolutionWindowedWidth";
        const string RESOLUTION_WINDOWED_HEIGHT_KEY = "resolutionWindowedHeight";

        // Parameters
        const float MIN_VOLUME = 0f;
        const float MAX_VOLUME = 1f;

        public static void SaveToDisk()
        {
            PlayerPrefs.Save();
        }

        public static void SetMasterVolume(float volume)
        {
            PlayerPrefs.SetFloat(MASTER_VOLUME_KEY, Mathf.Clamp(volume, MIN_VOLUME, MAX_VOLUME));
        }

        public static void SetBackgroundVolume(float volume)
        {
            PlayerPrefs.SetFloat(BACKGROUND_VOLUME_KEY, Mathf.Clamp(volume, MIN_VOLUME, MAX_VOLUME));
        }

        public static void SetSoundEffectsVolume(float volume)
        {
            PlayerPrefs.SetFloat(SOUND_EFFECTS_VOLUME_KEY, Mathf.Clamp(volume, MIN_VOLUME, MAX_VOLUME));
        }

        public static void SetResolutionSettings(ResolutionSetting resolutionSetting)
        {
            bool fullScreenWindowed = resolutionSetting.fullScreenMode == FullScreenMode.FullScreenWindow; 

            PlayerPrefs.SetInt(RESOLUTION_FULL_SCREEN_WINDOWED_KEY, fullScreenWindowed ? 1 : 0);
            if (fullScreenWindowed)
            {
                PlayerPrefs.SetInt(RESOLUTION_FSW_WIDTH_KEY, resolutionSetting.width);
                PlayerPrefs.SetInt(RESOLUTION_FSW_HEIGHT_KEY, resolutionSetting.height);
            }
            else
            {
                PlayerPrefs.SetInt(RESOLUTION_WINDOWED_WIDTH_KEY, resolutionSetting.width);
                PlayerPrefs.SetInt(RESOLUTION_WINDOWED_HEIGHT_KEY, resolutionSetting.height);
            }
        }

        public static float GetMasterVolume()
        {
            return PlayerPrefs.GetFloat(MASTER_VOLUME_KEY);
        }

        public static float GetBackgroundVolume()
        {
            return PlayerPrefs.GetFloat(BACKGROUND_VOLUME_KEY);
        }

        public static float GetSoundEffectsVolume()
        {
            return PlayerPrefs.GetFloat(SOUND_EFFECTS_VOLUME_KEY);
        }

        public static bool GetResolutionFullScreenWindowed()
        {
            return PlayerPrefs.GetInt(RESOLUTION_FULL_SCREEN_WINDOWED_KEY) == 1;
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
                    width = PlayerPrefs.GetInt(RESOLUTION_FSW_WIDTH_KEY);
                    height = PlayerPrefs.GetInt(RESOLUTION_FSW_HEIGHT_KEY);
                }
            }
            else
            {
                fullScreenMode = FullScreenMode.Windowed;

                if (ResolutionWindowedWidthKeyExists() && ResolutionWindowedHeightKeyExists())
                {
                    width = PlayerPrefs.GetInt(RESOLUTION_WINDOWED_WIDTH_KEY);
                    height = PlayerPrefs.GetInt(RESOLUTION_WINDOWED_HEIGHT_KEY);
                }
            }

            return new ResolutionSetting(fullScreenMode, width, height);
        }

        public static bool MasterVolumeKeyExists()
        {
            return PlayerPrefs.HasKey(MASTER_VOLUME_KEY);
        }

        public static bool BackgroundVolumeKeyExists()
        {
            return PlayerPrefs.HasKey(BACKGROUND_VOLUME_KEY);
        }

        public static bool SoundEffectsVolumeKeyExists()
        {
            return PlayerPrefs.HasKey(SOUND_EFFECTS_VOLUME_KEY);
        }

        public static bool ResolutionFullScreenWindowedKeyExists()
        {
            return PlayerPrefs.HasKey(RESOLUTION_FULL_SCREEN_WINDOWED_KEY);
        }

        public static bool ResolutionFSWWidthKeyExists()
        {
            return PlayerPrefs.HasKey(RESOLUTION_FSW_WIDTH_KEY);
        }

        public static bool ResolutionFSWHeightKeyExists()
        {
            return PlayerPrefs.HasKey(RESOLUTION_FSW_HEIGHT_KEY);
        }

        public static bool ResolutionWindowedWidthKeyExists()
        {
            return PlayerPrefs.HasKey(RESOLUTION_WINDOWED_WIDTH_KEY);
        }

        public static bool ResolutionWindowedHeightKeyExists()
        {
            return PlayerPrefs.HasKey(RESOLUTION_WINDOWED_HEIGHT_KEY);
        }
    }
}
