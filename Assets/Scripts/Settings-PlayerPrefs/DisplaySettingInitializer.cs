using UnityEngine;
using System.Collections;

namespace Frankie.Settings
{
    public class DisplaySettingInitializer : MonoBehaviour
    {
       [Tooltip("Enable if using PixelArtShaderBNN")][SerializeField] bool setPixelsPerTexel = false;

        private void Start()
        {
            if (setPixelsPerTexel)
            {
                float[] pixelsPerTexel = DisplayResolutions.GetPixelsPerTexel();
                Shader.SetGlobalFloatArray("_PixelsPerTexel", pixelsPerTexel);
            }

            if (SkipWindowAdjustment()) { return; }
            ResolutionSetting resolutionSetting = DisplayResolutions.GetBestWindowedResolution(1)[0];
            StartCoroutine(WaitForScreenChange(resolutionSetting));
        }

        private void OnDestroy()
        {
            SaveCurrentResolution();
        }

        private IEnumerator WaitForScreenChange(ResolutionSetting resolutionSetting)
        {
            yield return DisplayResolutions.UpdateScreenResolution(resolutionSetting);
            DisplayResolutions.SetWindowToCenter();
            PlayerPrefsController.SetResolutionSettings(resolutionSetting);
            PlayerPrefsController.SaveToDisk();
        }

        private bool SkipWindowAdjustment()
        {
            if (PlayerPrefsController.ResolutionInitializedKeyExists() && !PlayerPrefsController.HasCurrentDisplayChanged())
            {
                return true;
            }
            return false;
        }

        private void SaveCurrentResolution()
        {
            ResolutionSetting resolutionSetting = DisplayResolutions.GetCurrentResolution();
            PlayerPrefsController.SetResolutionSettings(resolutionSetting);
            PlayerPrefsController.SaveToDisk();
        }
    }
}
