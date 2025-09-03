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
    }
}
