using NUnit.Framework;
using UnityEngine;
using LowDefMustard.Zones.Editor;

namespace LowDefMustard.Zones.Tests.Editor
{
    public class MultiZoneViewerCaptureTests
    {
        // State
        private MultiZoneViewer viewer;
        private GameObject cameraGameObject;
        private Texture2D resultTexture;
        private MultiZoneView multiZoneView;

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
            Object.DestroyImmediate(viewer);
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

            Assert.AreEqual(new Vector2(10f, 10f), result);
        }

        [Test]
        public void GetIdealSnapshotDimensions_BelowMinimum_ScalesUpPreservingAspect()
        {
            // 10x10 world size * 80 scaling = 800x800, below the 1920x1080 minimum on both axes
            Vector2 result = viewer.GetIdealSnapshotDimensions(10f, 10f);

            Assert.AreEqual(new Vector2(1920f, 1920f), result);
        }

        [Test]
        public void GetIdealSnapshotDimensions_AboveMaximum_ClampsToMaxWithoutPreservingAspect()
        {
            // 200x200 world size * 80 scaling = 16000x16000, above the 7680x4320 maximum on both axes
            Vector2 result = viewer.GetIdealSnapshotDimensions(200f, 200f);

            Assert.AreEqual(new Vector2(7680f, 4320f), result);
        }

        [Test]
        public void PositionCameraToFrameScene_CentersCameraAndSetsOrthoSize()
        {
            cameraGameObject = new GameObject("Cam", typeof(Camera));
            var camera = cameraGameObject.GetComponent<Camera>();
            camera.transform.position = new Vector3(0f, 0f, -10f);
            var zoneBounds = new Bounds(new Vector3(5f, 7f, 0f), new Vector3(20f, 20f, 0f));

            viewer.PositionCameraToFrameScene(camera, zoneBounds);

            Assert.AreEqual(new Vector3(5f, 7f, -10f), camera.transform.position);
            Assert.AreEqual(10f, camera.orthographicSize);
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
        #endregion
    }
}
