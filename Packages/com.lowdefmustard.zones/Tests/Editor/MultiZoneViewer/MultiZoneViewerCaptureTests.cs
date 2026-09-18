using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using LowDefMustard.Zones.Editor;

namespace LowDefMustard.Zones.Tests.Editor
{
    public class MultiZoneViewerCaptureTests
    {
        // Const Tunables
        private const float _tolerance = 0.01f;

        // State
        private MultiZoneViewer viewer;
        private GameObject cameraGameObject;
        private Texture2D resultTexture;
        private MultiZoneView multiZoneView;
        private GameObject cubeA;
        private GameObject cubeB;
        private bool createdScratchScene;

        #region Setup
        [SetUp]
        public void SetUp()
        {
            viewer = ScriptableObject.CreateInstance<MultiZoneViewer>();
        }

        [TearDown]
        public void TearDown()
        {
            if (resultTexture != null) { Object.DestroyImmediate(resultTexture); }
            if (cameraGameObject != null) { Object.DestroyImmediate(cameraGameObject); }
            if (multiZoneView != null) { Object.DestroyImmediate(multiZoneView); }
            if (cubeA != null) { Object.DestroyImmediate(cubeA); }
            if (cubeB != null) { Object.DestroyImmediate(cubeB); }
            Object.DestroyImmediate(viewer);

            // Discards the scratch scene (and anything left in it) by replacing it with a fresh one
            if (createdScratchScene)
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                createdScratchScene = false;
            }
        }

        private static void AssertVector2Approximately(Vector2 expected, Vector2 actual)
        {
            Assert.AreEqual(expected.x, actual.x, _tolerance);
            Assert.AreEqual(expected.y, actual.y, _tolerance);
        }

        private static void AssertVector3Approximately(Vector3 expected, Vector3 actual)
        {
            Assert.AreEqual(expected.x, actual.x, _tolerance);
            Assert.AreEqual(expected.y, actual.y, _tolerance);
            Assert.AreEqual(expected.z, actual.z, _tolerance);
        }
        #endregion

        #region Tests
        [Test]
        public void CameraClick_ReturnsTextureWithRequestedDimensions()
        {
            cameraGameObject = new GameObject("Cam", typeof(Camera));
            var camera = cameraGameObject.GetComponent<Camera>();

            resultTexture = MultiZoneViewer.CameraClick(camera, new Vector2(64, 48));

            Assert.AreEqual(64, resultTexture.width);
            Assert.AreEqual(48, resultTexture.height);
        }

        [Test]
        public void GetIdealSnapshotDimensions_ZeroWorldSize_ReturnsDummyDimensions()
        {
            Vector2 result = viewer.GetIdealSnapshotDimensions(0f, 5f);

            AssertVector2Approximately(new Vector2(10f, 10f), result);
        }

        [Test]
        public void GetIdealSnapshotDimensions_BelowMinimum_ScalesUpPreservingAspect()
        {
            // 10x10 world size * 80 scaling = 800x800, below the 1920x1080 minimum on both axes
            Vector2 result = viewer.GetIdealSnapshotDimensions(10f, 10f);

            AssertVector2Approximately(new Vector2(1920f, 1920f), result);
        }

        [Test]
        public void GetIdealSnapshotDimensions_AboveMaximum_ClampsToMaxWithoutPreservingAspect()
        {
            // 200x200 world size * 80 scaling = 16000x16000, above the 7680x4320 maximum on both axes
            Vector2 result = viewer.GetIdealSnapshotDimensions(200f, 200f);

            AssertVector2Approximately(new Vector2(7680f, 4320f), result);
        }

        [Test]
        public void PositionCameraToFrameScene_CentersCameraAndSetsOrthoSize()
        {
            cameraGameObject = new GameObject("Cam", typeof(Camera));
            var camera = cameraGameObject.GetComponent<Camera>();
            camera.transform.position = new Vector3(0f, 0f, -10f);
            var zoneBounds = new Bounds(new Vector3(5f, 7f, 0f), new Vector3(20f, 20f, 0f));

            viewer.PositionCameraToFrameScene(camera, zoneBounds);

            AssertVector3Approximately(new Vector3(5f, 7f, -10f), camera.transform.position);
            Assert.AreEqual(10f, camera.orthographicSize, _tolerance);
        }

        [Test]
        public void CaptureZone_NoActiveMultiZoneView_ReturnsNull()
        {
            viewer.activeMultiZoneView = null;

            resultTexture = viewer.CaptureZone(new Bounds(Vector3.zero, Vector3.one));

            Assert.IsNull(resultTexture);
        }

        [Test]
        public void CaptureZone_WithMainCamera_ReturnsRenderedTexture()
        {
            multiZoneView = ScriptableObject.CreateInstance<MultiZoneView>();
            viewer.activeMultiZoneView = multiZoneView;

            cameraGameObject = new GameObject("MainCam", typeof(Camera)) { tag = "MainCamera" };

            var zoneBounds = new Bounds(new Vector3(5f, 7f, 0f), new Vector3(20f, 20f, 0f));
            resultTexture = viewer.CaptureZone(zoneBounds);

            Assert.IsNotNull(resultTexture);
            Assert.AreEqual(1920, resultTexture.width);
            Assert.AreEqual(1920, resultTexture.height);
        }

        [Test]
        public void CalculateZoneBounds_WithRenderers_EncapsulatesAllRendererBounds()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            createdScratchScene = true;

            cubeA = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cubeA.transform.position = Vector3.zero;
            cubeB = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cubeB.transform.position = new Vector3(10f, 0f, 0f);

            // Default Cube primitives are unit cubes, so each renderer's world-space bounds are exactly (position, (1,1,1))
            //  - the union of a cube at (0,0,0) and one at (10,0,0) is center (5,0,0) -> size (11,1,1)
            var expected = new Bounds(new Vector3(5f, 0f, 0f), new Vector3(11f, 1f, 1f));

            Bounds result = MultiZoneViewer.CalculateZoneBounds();

            AssertVector3Approximately(expected.center, result.center);
            AssertVector3Approximately(expected.size, result.size);
        }
        #endregion
    }
}
