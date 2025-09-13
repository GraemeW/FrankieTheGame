using UnityEngine;
using System.Collections;

namespace Frankie.Settings
{
    public class DisplaySettingMonitor : MonoBehaviour
    {
        private void Start()
        {
            if (SkipWindowAdjustment()) { return; }
            ResolutionSetting resolutionSetting = DisplayResolutions.GetBestWindowedResolution(1)[0];
            StartCoroutine(WaitForScreenChange(resolutionSetting));
        }

        private void OnDestroy()
        {
            SaveResolutionSetting(DisplayResolutions.GetCurrentResolution());
        }

        private void Update()
        {
            DisplayResolutions.CheckForResolutionChange();
        }

        private IEnumerator WaitForScreenChange(ResolutionSetting resolutionSetting)
        {
            yield return DisplayResolutions.UpdateScreenResolution(resolutionSetting);
            DisplayResolutions.SetWindowToCenter();
            SaveResolutionSetting(resolutionSetting);
        }

        private bool SkipWindowAdjustment()
        {
            if (PlayerPrefsController.ResolutionInitializedKeyExists() && !PlayerPrefsController.HasCurrentDisplayChanged())
            {
                return true;
            }
            return false;
        }

        private void SaveResolutionSetting(ResolutionSetting resolutionSetting)
        {
            PlayerPrefsController.SetResolutionSettings(resolutionSetting);
            PlayerPrefsController.SaveToDisk();
        }
    }
}
