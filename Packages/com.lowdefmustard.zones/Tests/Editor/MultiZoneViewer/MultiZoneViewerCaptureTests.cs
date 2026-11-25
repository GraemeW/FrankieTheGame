using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using LowDefMustard.Utils.Tests;
using LowDefMustard.Zones.Editor;

namespace LowDefMustard.Zones.Tests.Editor
{
    public class MultiZoneViewerCaptureTests
    {
        // Const Tunables
        private const float _tolerance = 0.01f;

        // State
        private readonly TestObjectTracker tracker = new();
        private MultiZoneViewer viewer;
        private MultiZoneView multiZoneView;
        private Tile spawnedTile;
        private bool spawnedScratchScene;

        #region Setup
        [SetUp]
        public void SetUp()
        {
            viewer = ScriptableObject.CreateInstance<MultiZoneViewer>();
        }

        [TearDown]
        public void TearDown()
        {
            tracker.DestroyAll();
            Object.DestroyImmediate(viewer);

            // Discards the scratch scene (and anything left in it) by replacing it with a fresh one
            if (spawnedScratchScene)
            {
                SceneTestUtility.ResetToEmptyScene();
                spawnedScratchScene = false;
            }
        }
        #endregion
        
        #region PrivateMethods
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

        private GameObject CreateCube(Vector3 position)
        {
            GameObject cube = tracker.Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            cube.transform.position = position;
            return cube;
        }

        private Tilemap CreateTilemap(Vector3 gridPosition, string suffix = "")
        {
            GameObject gridGameObject = tracker.CreateGameObject($"Grid{suffix}", typeof(Grid));

            var tilemapGameObject = new GameObject($"Tilemap{suffix}", typeof(Tilemap), typeof(TilemapRenderer));
            tilemapGameObject.transform.SetParent(gridGameObject.transform);
            gridGameObject.transform.position = gridPosition; // Must be done after tilemap added to shift tilemap with the grid
            var tilemap = tilemapGameObject.GetComponent<Tilemap>();
            return tilemap;
        }
        #endregion

        #region Tests
        [Test]
        public void CameraClick_ReturnsTextureWithRequestedDimensions()
        {
            var camera = tracker.CreateComponent<Camera>("Cam");

            Texture2D resultTexture = tracker.Track(MultiZoneViewer.CameraClick(camera, new Vector2(64, 48)));

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
            var camera = tracker.CreateComponent<Camera>("Cam");
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

            Texture2D resultTexture = tracker.Track(viewer.CaptureZone(new Bounds(Vector3.zero, Vector3.one)));

            Assert.IsNull(resultTexture);
        }

        [Test]
        public void CaptureZone_WithMainCamera_ReturnsRenderedTexture()
        {
            multiZoneView = tracker.Create<MultiZoneView>();
            viewer.activeMultiZoneView = multiZoneView;

            tracker.Track(new GameObject("MainCam", typeof(Camera)) { tag = "MainCamera" });

            var zoneBounds = new Bounds(new Vector3(5f, 7f, 0f), new Vector3(20f, 20f, 0f));
            Texture2D resultTexture = tracker.Track(viewer.CaptureZone(zoneBounds));

            Assert.IsNotNull(resultTexture);
            Assert.AreEqual(1920, resultTexture.width);
            Assert.AreEqual(1920, resultTexture.height);
        }

        [Test]
        public void CalculateZoneBounds_WithRenderers_EncapsulatesAllRendererBounds()
        {
            SceneTestUtility.ResetToEmptyScene();
            spawnedScratchScene = true;

            CreateCube(Vector3.zero);
            CreateCube(new Vector3(10f, 0f, 0f));

            // Default Cube primitives are unit cubes, so each renderer's world-space bounds are exactly (position, (1,1,1))
            //  - the union of a cube at (0,0,0) and one at (10,0,0) is centre (5,0,0) -> size (11,1,1)
            var expected = new Bounds(new Vector3(5f, 0f, 0f), new Vector3(11f, 1f, 1f));

            Bounds result = MultiZoneViewer.CalculateZoneBounds();

            AssertVector3Approximately(expected.center, result.center);
            AssertVector3Approximately(expected.size, result.size);
        }

        [Test]
        public void CalculateZoneBounds_WithSingleTilemap_UsesCompressedCellBoundsInWorldSpace()
        {
            SceneTestUtility.ResetToEmptyScene();
            spawnedScratchScene = true;

            Tilemap tilemap = CreateTilemap(Vector3.zero);

            spawnedTile = tracker.Create<Tile>();
            tilemap.SetTile(new Vector3Int(0, 0, 0), spawnedTile);
            tilemap.SetTile(new Vector3Int(3, 2, 0), spawnedTile);
            tilemap.CompressBounds();

            // Tiles at cell (0,0) and (3,2) compress to cellBounds min (0,0,0), max (4,3,1)
            //  -> with the default Grid cell size of (1,1,1) and an identity transform, CellToWorld maps those straight to world (0,0,0) and (4,3,1)
            var expected = new Bounds(new Vector3(2f, 1.5f, 0f), new Vector3(4f, 3f, 0f));

            Bounds result = MultiZoneViewer.CalculateZoneBounds();

            AssertVector3Approximately(expected.center, result.center);
            AssertVector3Approximately(expected.size, result.size);
        }

        [Test]
        public void CalculateZoneBounds_WithMultipleTilemaps_EncapsulatesAllTilemapBounds()
        {
            SceneTestUtility.ResetToEmptyScene();
            spawnedScratchScene = true;
            spawnedTile = tracker.Create<Tile>();

            // Tilemap A: a single tilemap spanning cells (0,0) to (3,2), at the world origin
            Tilemap tilemapA = CreateTilemap(Vector3.zero, "A");
            tilemapA.SetTile(new Vector3Int(0, 0, 0), spawnedTile);
            tilemapA.SetTile(new Vector3Int(3, 2, 0), spawnedTile);
            tilemapA.CompressBounds();

            // Tilemap B: a single tile at its own cell origin, but its Grid is moved to world (10,0,0)
            //  -> so the tilemap's world-space cell bounds become (10,0,0) to (11,1,0)
            Tilemap tilemapB = CreateTilemap(new Vector3(10f, 0f, 0f), "B");
            tilemapB.SetTile(new Vector3Int(0, 0, 0), spawnedTile);
            tilemapB.CompressBounds();

            // Union of (0,0,0)-(4,3,0) and (10,0,0)-(11,1,0) is (0,0,0)-(11,3,0)
            var expected = new Bounds(new Vector3(5.5f, 1.5f, 0f), new Vector3(11f, 3f, 0f));

            Bounds result = MultiZoneViewer.CalculateZoneBounds();

            AssertVector3Approximately(expected.center, result.center);
            AssertVector3Approximately(expected.size, result.size);
        }
        #endregion
    }
}
