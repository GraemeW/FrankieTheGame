using LowDefMustard.Rendering;
using LowDefMustard.Zones;
using Frankie.Saving;

namespace Frankie.Rendering
{
    public sealed class DisplaySettingMonitor : DisplaySettingMonitorBase
    {
        // Note:  Resolution persists in player prefs && re-announced on every zone change

        #region UnityMethods
        private void OnEnable()
        {
            SceneLoaderBase.zoneUpdated -= TriggerResolutionAnnounce;
            SceneLoaderBase.zoneUpdated += TriggerResolutionAnnounce;
        }

        private void OnDisable()
        {
            SceneLoaderBase.zoneUpdated -= TriggerResolutionAnnounce;
        }
        #endregion

        #region AbstractImplementations
        protected override bool IsResolutionAlreadyConfigured() => PlayerPrefsController.ResolutionInitializedKeyExists() && !PlayerPrefsController.HasCurrentDisplayChanged();

        protected override void SaveResolutionSetting(ResolutionSetting resolutionSetting)
        {
            PlayerPrefsController.SetResolutionSettings(resolutionSetting);
            PlayerPrefsController.SaveToDisk();
        }
        #endregion

        #region PrivateMethods
        private static void TriggerResolutionAnnounce(Zone zone) => DisplayResolutions.AnnounceResolution();
        #endregion
    }
}
