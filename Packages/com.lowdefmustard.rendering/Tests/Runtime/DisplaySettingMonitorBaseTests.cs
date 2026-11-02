#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LowDefMustard.Rendering.Tests
{
    public class DisplaySettingMonitorBaseTests
    {
        // Notes:
        //  - Only the already-configured path runs:  first-run setup calls Screen.SetResolution + Screen.MoveMainWindowTo, which would resize / move the real editor window
        //  - Update's change polling can't be driven (the screen size can't be changed from a test) ~ see DisplayResolutionsTests for CheckForResolutionChange

        // State
        private GameObject monitorObject;
        private TestDisplaySettingMonitor monitor;

        #region Setup
        [SetUp]
        public void SetUp()
        {
            monitorObject = new GameObject("_TEMP_DisplaySettingMonitor");
            monitorObject.SetActive(false);
            monitor = monitorObject.AddComponent<TestDisplaySettingMonitor>();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (monitorObject != null) { Object.Destroy(monitorObject); }
            yield return null;
        }
        #endregion

        #region Tests
        [UnityTest]
        public IEnumerator Start_AlreadyConfigured_LeavesTheResolutionAlone()
        {
            monitorObject.SetActive(true);
            yield return null;
            yield return null;

            Assert.IsEmpty(monitor.savedSettings);
        }

        [UnityTest]
        public IEnumerator OnDestroy_SavesTheCurrentResolution()
        {
            monitorObject.SetActive(true);
            yield return null;
            ResolutionSetting currentResolution = DisplayResolutions.GetCurrentResolution();

            Object.Destroy(monitorObject);
            yield return null;

            Assert.AreEqual(1, monitor.savedSettings.Count);
            Assert.AreEqual(currentResolution, monitor.savedSettings[0]);
        }
        #endregion
    }
}
#endif
