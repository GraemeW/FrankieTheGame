#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;

namespace LowDefMustard.Rendering.Tests
{
    public class CameraControllerBaseTests
    {
        // Notes:
        //  - PlayMode:  OnEnable / Start / OnDisable drive the controller
        //  - Ortho sizes are checked as ratios + against the raised value (the scaler depends on the editor's screen mode)

        // Const Tunables
        private const string _cameraControllerTag = "CameraController";
        private const float _defaultActiveOrthoSize = 3.6f;
        private const float _defaultIdleOrthoSize = 1.8f;

        // State
        private CameraRig rig;
        private readonly List<GameObject> extraObjects = new();

        #region Setup
        [SetUp]
        public void SetUp()
        {
            rig = new CameraRig();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            rig.Destroy();
            foreach (GameObject extraObject in extraObjects) { Object.Destroy(extraObject); }
            extraObjects.Clear();
            yield return null;
            rig = null;
        }
        #endregion

        #region PrivateMethods
        private GameObject CreateExtraTarget(string name)
        {
            GameObject extraTarget = CameraRig.CreateTarget(name);
            extraObjects.Add(extraTarget);
            return extraTarget;
        }

        private void AssertFollowing(GameObject expectedTarget)
        {
            Assert.AreSame(expectedTarget.transform, rig.activeCamera.Follow, "active camera");
            Assert.AreSame(expectedTarget.transform, rig.idleCamera.Follow, "idle camera");
            Assert.AreSame(expectedTarget.GetComponent<Animator>(), rig.stateCamera.AnimatedTarget, "state-driven camera");
        }
        #endregion

        #region Lifecycle
        [UnityTest]
        public IEnumerator Enable_SubscribesAndFollowsTheDefaultTarget()
        {
            rig.Activate();
            yield return null;

            CollectionAssert.AreEqual(new[] { true }, rig.controller.subscriptions);
            AssertFollowing(rig.target);
        }

        [UnityTest]
        public IEnumerator Start_NoTargetOnEnable_FindsItAgain()
        {
            rig.controller.queuedTargets.Enqueue(null);

            rig.Activate();
            yield return null;

            AssertFollowing(rig.target);
        }

        [UnityTest]
        public IEnumerator Start_StacksTheSpawnAssistCameraOntoTheMainCamera()
        {
            rig.Activate();
            yield return null;

            CollectionAssert.AreEqual(new[] { rig.spawnAssistCamera }, rig.mainCamera.GetUniversalAdditionalCameraData().cameraStack);
        }

        [UnityTest]
        public IEnumerator Disable_UnsubscribesAndStopsFollowingResolutionChanges()
        {
            rig.Activate();
            yield return null;
            int raisedCount = 0;
            rig.controller.activeOrthoSizeUpdated += _ => raisedCount++;

            rig.root.SetActive(false);
            DisplayResolutions.AnnounceResolution();

            CollectionAssert.AreEqual(new[] { true, false }, rig.controller.subscriptions);
            Assert.AreEqual(0, raisedCount);
        }
        #endregion

        #region Following
        [UnityTest]
        public IEnumerator SetDefaultFollowTarget_RetargetsEveryCamera()
        {
            rig.Activate();
            yield return null;
            GameObject newLeader = CreateExtraTarget("NewLeader");

            rig.controller.AnnounceDefaultFollowTarget(newLeader);

            AssertFollowing(newLeader);
        }

        [UnityTest]
        public IEnumerator SetDefaultFollowTarget_Null_KeepsTheCurrentTarget()
        {
            rig.Activate();
            yield return null;

            rig.controller.AnnounceDefaultFollowTarget(null);

            AssertFollowing(rig.target);
        }

        [UnityTest]
        public IEnumerator OverrideCameraFollower_ThenRefresh_ReturnsToTheDefaultTarget()
        {
            rig.Activate();
            yield return null;
            GameObject cutsceneTarget = CreateExtraTarget("CutsceneTarget");

            rig.controller.OverrideCameraFollower(cutsceneTarget.GetComponent<Animator>(), cutsceneTarget.transform);
            AssertFollowing(cutsceneTarget);

            rig.controller.RefreshDefaultCameras();
            AssertFollowing(rig.target);
        }
        #endregion

        #region OrthoSize
        [UnityTest]
        public IEnumerator ResolutionUpdated_RescalesBothCamerasAndRaisesTheActiveSize()
        {
            rig.Activate();
            yield return null;
            float? raisedSize = null;
            rig.controller.activeOrthoSizeUpdated += orthoSize => raisedSize = orthoSize;

            DisplayResolutions.AnnounceResolution();

            Assert.IsTrue(raisedSize.HasValue);
            Assert.AreEqual(raisedSize.Value, rig.controller.GetActiveOrthoSize());
            Assert.AreEqual(raisedSize.Value, rig.activeCamera.Lens.OrthographicSize, 1e-5f);
            Assert.AreEqual(_defaultActiveOrthoSize / _defaultIdleOrthoSize, rig.activeCamera.Lens.OrthographicSize / rig.idleCamera.Lens.OrthographicSize, 1e-4f);
        }

        [Test]
        public void GetActiveOrthoSize_BeforeAnyResolutionUpdate_IsTheDefault()
        {
            Assert.AreEqual(_defaultActiveOrthoSize, rig.controller.GetActiveOrthoSize());
        }
        #endregion

        #region Static
        [UnityTest]
        public IEnumerator GetCameraController_FindsTheTaggedController()
        {
            // Project-dependent:  the CameraController tag must be defined in the project's tags
            try { rig.root.tag = _cameraControllerTag; }
            catch (UnityException) { Assert.Ignore($"No {_cameraControllerTag} tag in this project"); }
            rig.Activate();
            yield return null;

            Assert.AreSame(rig.controller, CameraControllerBase.GetCameraController());
        }
        #endregion
    }
}
#endif
