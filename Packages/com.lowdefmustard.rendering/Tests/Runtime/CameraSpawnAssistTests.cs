#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace LowDefMustard.Rendering.Tests
{
    public class CameraSpawnAssistTests
    {
        // Const Tunables
        private const float _defaultMultiplier = 1.25f;

        // State
        private CameraRig rig;
        private CameraSpawnAssist spawnAssist;

        #region Setup
        [SetUp]
        public void SetUp()
        {
            rig = new CameraRig();
            spawnAssist = rig.spawnAssistCamera.gameObject.AddComponent<CameraSpawnAssist>();
            SerializedFieldInjector.Set(spawnAssist, "cameraController", rig.controller);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            rig.Destroy();
            yield return null;
            rig = null;
        }
        #endregion

        #region Tests
        [UnityTest]
        public IEnumerator Start_MatchesTheActiveOrthoSizeTimesTheMultiplier()
        {
            rig.Activate();
            yield return null;

            Assert.AreEqual(rig.controller.GetActiveOrthoSize() * _defaultMultiplier, rig.spawnAssistCamera.orthographicSize, 1e-4f);
        }

        [UnityTest]
        public IEnumerator ActiveOrthoSizeUpdated_FollowsTheController()
        {
            rig.Activate();
            yield return null;

            DisplayResolutions.AnnounceResolution();

            Assert.AreEqual(rig.controller.GetActiveOrthoSize() * _defaultMultiplier, rig.spawnAssistCamera.orthographicSize, 1e-4f);
        }

        [UnityTest]
        public IEnumerator Disabled_StopsFollowingTheController()
        {
            rig.Activate();
            yield return null;
            spawnAssist.enabled = false;
            rig.spawnAssistCamera.orthographicSize = 99f;

            DisplayResolutions.AnnounceResolution();

            Assert.AreEqual(99f, rig.spawnAssistCamera.orthographicSize);
        }

        [UnityTest]
        public IEnumerator NoController_DoesNothing()
        {
            SerializedFieldInjector.Set(spawnAssist, "cameraController", null);
            rig.spawnAssistCamera.orthographicSize = 99f;

            rig.Activate();
            yield return null;
            DisplayResolutions.AnnounceResolution();

            Assert.AreEqual(99f, rig.spawnAssistCamera.orthographicSize);
        }
        #endregion
    }
}
#endif
