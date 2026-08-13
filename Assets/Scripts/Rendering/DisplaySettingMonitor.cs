using System.Collections;
using UnityEngine;
using LowDefMustard.Zones;
using Frankie.Saving;

namespace Frankie.Rendering
{
    public class DisplaySettingMonitor : MonoBehaviour
    {
        #region StaticMethods
        private static bool SkipWindowAdjustment() => PlayerPrefsController.ResolutionInitializedKeyExists() && !PlayerPrefsController.HasCurrentDisplayChanged();
        
        private static void TriggerResolutionAnnounce(Zone zone)
        {
            DisplayResolutions.AnnounceResolution();
        }
        
        private static IEnumerator WaitForScreenChange(ResolutionSetting resolutionSetting)
        {
            yield return DisplayResolutions.UpdateScreenResolution(resolutionSetting);
            DisplayResolutions.SetWindowToCenter();
            SaveResolutionSetting(resolutionSetting);
        }

        private static void SaveResolutionSetting(ResolutionSetting resolutionSetting)
        {
            PlayerPrefsController.SetResolutionSettings(resolutionSetting);
            PlayerPrefsController.SaveToDisk();
        }
        #endregion
        
        #region UnityMethods
        private void Start()
        {
            if (SkipWindowAdjustment()) { return; }
            ResolutionSetting resolutionSetting = DisplayResolutions.GetBestWindowedResolution(1)[0];
            StartCoroutine(WaitForScreenChange(resolutionSetting));
        }

        private void OnEnable()
        {
            SceneLoaderBase.zoneUpdated += TriggerResolutionAnnounce;

        }

        private void OnDisable()
        {
            SceneLoaderBase.zoneUpdated -= TriggerResolutionAnnounce;
        }

        private void OnDestroy()
        {
            SaveResolutionSetting(DisplayResolutions.GetCurrentResolution());
        }

        private void Update()
        {
            DisplayResolutions.CheckForResolutionChange();
        }
        #endregion
    }
}
