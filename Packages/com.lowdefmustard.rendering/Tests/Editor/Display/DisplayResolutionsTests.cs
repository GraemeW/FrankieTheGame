using NUnit.Framework;
using UnityEngine;

namespace LowDefMustard.Rendering.Tests.Editor
{
    public class DisplayResolutionsTests
    {
        // Notes:
        //  - DisplayResolutions reads Screen / Screen.mainWindowDisplayInfo directly (cannot be isolated) ~ tests below thus assert invariants that hold on any display
        //  - Never calls UpdateScreenResolution / ForceScreenResolution / SetWindowToCenter since they'd resize or move the real editor window
        //  - In the editor, camera scaling is fixed at 2

        // Const Tunables
        private const float _editorCameraScaling = 2f;

        // State
        private int announceCount;
        private float announcedCameraScaling;

        #region Setup
        [SetUp]
        public void SetUp()
        {
            announceCount = 0;
            DisplayResolutions.resolutionUpdated += HandleResolutionUpdated;
        }

        [TearDown]
        public void TearDown()
        {
            DisplayResolutions.resolutionUpdated -= HandleResolutionUpdated;
        }
        #endregion

        #region PrivateMethods
        private void HandleResolutionUpdated(ResolutionScaler resolutionScaler, float cameraScaling)
        {
            announceCount++;
            announcedCameraScaling = cameraScaling;
        }
        #endregion

        #region Events
        [Test]
        public void AnnounceResolution_RaisesResolutionUpdated()
        {
            DisplayResolutions.AnnounceResolution();

            Assert.AreEqual(1, announceCount);
            Assert.AreEqual(_editorCameraScaling, announcedCameraScaling);
        }

        [Test]
        public void CheckForResolutionChange_Unchanged_DoesNotRaise()
        {
            DisplayResolutions.CheckForResolutionChange(); // Syncs to the current screen (may raise once)
            announceCount = 0;

            DisplayResolutions.CheckForResolutionChange();

            Assert.AreEqual(0, announceCount);
        }
        #endregion

        #region Resolutions
        [Test]
        public void GetCurrentResolution_MirrorsTheScreen()
        {
            ResolutionSetting resolutionSetting = DisplayResolutions.GetCurrentResolution();

            Assert.AreEqual(Screen.fullScreenMode, resolutionSetting.fullScreenMode);
            Assert.AreEqual(Screen.width, resolutionSetting.width);
            Assert.AreEqual(Screen.height, resolutionSetting.height);
        }

        [TestCase(1)]
        [TestCase(5)]
        [TestCase(100)]
        public void GetBestWindowedResolution_ReturnsWindowedOptionsThatFitTheDisplay(int count)
        {
            DisplayInfo displayInfo = Screen.mainWindowDisplayInfo;

            var resolutionSettings = DisplayResolutions.GetBestWindowedResolution(count);

            Assert.That(resolutionSettings.Count, Is.InRange(1, Mathf.Clamp(count, 1, 20)));
            foreach (ResolutionSetting resolutionSetting in resolutionSettings)
            {
                Assert.AreEqual(FullScreenMode.Windowed, resolutionSetting.fullScreenMode);
                Assert.LessOrEqual(resolutionSetting.width, displayInfo.width);
                Assert.LessOrEqual(resolutionSetting.height, displayInfo.height);
            }
        }

        [Test]
        public void GetBestWindowedResolution_ZeroCount_StillReturnsOne()
        {
            Assert.GreaterOrEqual(DisplayResolutions.GetBestWindowedResolution(0).Count, 1);
        }

        [TestCase(1920, 1080, 1920, 1080)]
        [TestCase(1920, 1200, 1920, 1200)]
        [TestCase(3440, 1440, 2560, 1440)]
        [TestCase(5120, 1440, 2560, 1440)]
        public void GetFullScreenWidthResolution_UltraWide_CapsTheWidthTo16By9(int displayWidth, int displayHeight, int expectedWidth, int expectedHeight)
        {
            ResolutionSetting resolutionSetting = DisplayResolutions.GetFullScreenWidthResolution(displayWidth, displayHeight);

            Assert.AreEqual(FullScreenMode.FullScreenWindow, resolutionSetting.fullScreenMode);
            Assert.AreEqual(expectedWidth, resolutionSetting.width);
            Assert.AreEqual(expectedHeight, resolutionSetting.height);
        }

        [Test]
        public void GetFullScreenWidthResolution_IsFullScreenWindowAndFitsTheDisplay()
        {
            DisplayInfo displayInfo = Screen.mainWindowDisplayInfo;

            ResolutionSetting resolutionSetting = DisplayResolutions.GetFullScreenWidthResolution();

            Assert.AreEqual(FullScreenMode.FullScreenWindow, resolutionSetting.fullScreenMode);
            Assert.LessOrEqual(resolutionSetting.width, displayInfo.width);
            Assert.LessOrEqual(resolutionSetting.height, displayInfo.height);
        }
        #endregion
    }
}
