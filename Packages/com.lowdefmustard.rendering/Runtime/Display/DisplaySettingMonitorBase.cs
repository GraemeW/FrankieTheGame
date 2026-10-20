using System.Collections;
using UnityEngine;

namespace LowDefMustard.Rendering
{
    public abstract class DisplaySettingMonitorBase : MonoBehaviour
    {
        // Notes:
        //  - Picks a best-fit windowed resolution on first run, then announces resolution changes (see DisplayResolutions)
        //  - Subclasses persist the resolution setting (e.g. to player prefs)

        #region AbstractMethods
        protected abstract bool IsResolutionAlreadyConfigured();
        protected abstract void SaveResolutionSetting(ResolutionSetting resolutionSetting);
        #endregion

        #region UnityMethods
        private void Start()
        {
            if (IsResolutionAlreadyConfigured()) { return; }
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
        #endregion

        #region PrivateMethods
        private IEnumerator WaitForScreenChange(ResolutionSetting resolutionSetting)
        {
            yield return DisplayResolutions.UpdateScreenResolution(resolutionSetting);
            DisplayResolutions.SetWindowToCenter();
            SaveResolutionSetting(resolutionSetting);
        }
        #endregion
    }
}
