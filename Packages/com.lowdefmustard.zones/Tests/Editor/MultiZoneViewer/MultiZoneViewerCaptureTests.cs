using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor.SceneManagement;
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
        private readonly List<GameObject> spawnedObjects = new();
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
            if (resultTexture != null) { Object.DestroyImmediate(resultTexture); }
            if (cameraGameObject != null) { Object.DestroyImmediate(cameraGameObject); }
            if (multiZoneView != null) { Object.DestroyImmediate(multiZoneView); }
            foreach (GameObject spawnedObject in spawnedObjects.Where(spawnedObject => spawnedObject != null))
            {
                Object.DestroyImmediate(spawnedObject);
            }
            if (spawnedTile != null) { Object.DestroyImmediate(spawnedTile); }
            Object.DestroyImmediate(viewer);

            // Discards the scratch scene (and anything left in it) by replacing it with a fresh one
            if (spawnedScratchScene)
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
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
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            spawnedObjects.Add(cube);
            
            cube.transform.position = position;
            return cube;
        }

        private Tilemap CreateTilemap(Vector3 gridPosition, string suffix = "")
        {
            var gridGameObject = new GameObject($"Grid{suffix}", typeof(Grid));
            spawnedObjects.Add(gridGameObject);
            
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
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            spawnedScratchScene = true;

            Tilemap tilemap = CreateTilemap(Vector3.zero);

            spawnedTile = ScriptableObject.CreateInstance<Tile>();
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
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            spawnedScratchScene = true;
            spawnedTile = ScriptableObject.CreateInstance<Tile>();

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
