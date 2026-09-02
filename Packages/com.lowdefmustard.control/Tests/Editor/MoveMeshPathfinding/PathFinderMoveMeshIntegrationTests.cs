using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace LowDefMustard.Control.Tests.Editor
{
    
    public class PathFinderMoveMeshIntegrationTests
    {
        // State
        private readonly List<GameObject> spawnedGameObjects = new();

        #region Setup
        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in spawnedGameObjects.Where(go => go != null))
            {
                Object.DestroyImmediate(go);
            }

            spawnedGameObjects.Clear();
        }
        #endregion

        #region PrivateMethods
        private GameObject CreateGameObject()
        {
            var go = new GameObject("PathFinderMoveMeshIntegrationTests_Target");
            spawnedGameObjects.Add(go);
            return go;
        }

        // Note: We place MoveMesh's GameObject at world origin w/ identity transform,
        //  - such that world space and the grid's local space are numerically identical
        //  - this keeps the expected values in each test provable by hand
        private MoveMesh CreateMoveMesh(int columns, int rows, float cellSize, bool[] cells)
        {
            var moveMesh = CreateGameObject().AddComponent<MoveMesh>();
            moveMesh.walkabilityGrid = new WalkabilityGrid
            {
                columns = columns,
                rows = rows,
                cellSize = cellSize,
                originX = 0f,
                originY = 0f,
                cells = new List<bool>(cells),
                traversalCosts = Enumerable.Repeat(1f, columns * rows).ToList()
            };
            moveMesh.gameObject.layer = MoveMesh.GetMoveMeshLayer();
            return moveMesh;
        }

        private PathFinder CreatePathFinder(MoveMesh moveMesh)
        {
            var pathFinder = CreateGameObject().AddComponent<PathFinder>();
            pathFinder.cachedMoveMesh = moveMesh;
            pathFinder.InitializePathfindingCache();
            return pathFinder;
        }

        private static bool[] AllWalkable(int columns, int rows) => Enumerable.Repeat(true, columns * rows).ToArray();
        #endregion

        #region WorldCellConversions
        [Test]
        public void CellToWorld_ReturnsCellCenterInWorldSpace()
        {
            MoveMesh moveMesh = CreateMoveMesh(5, 5, 1f, AllWalkable(5, 5));
            Vector2 world = moveMesh.CellToWorld(2, 3);
            Assert.That(world.x, Is.EqualTo(2.5f).Within(0.0001f));
            Assert.That(world.y, Is.EqualTo(3.5f).Within(0.0001f));
        }

        [Test]
        public void WorldToCell_RoundTripsWithCellToWorld()
        {
            MoveMesh moveMesh = CreateMoveMesh(5, 5, 1f, AllWalkable(5, 5));
            Vector2 world = moveMesh.CellToWorld(2, 3);
            bool isValid = moveMesh.WorldToCell(world, out int column, out int row);
            Assert.That(isValid, Is.True);
            Assert.That(column, Is.EqualTo(2));
            Assert.That(row, Is.EqualTo(3));
        }

        [Test]
        public void WorldToCell_OutsideGridBounds_ReturnsFalse()
        {
            MoveMesh moveMesh = CreateMoveMesh(5, 5, 1f, AllWalkable(5, 5));
            bool isValid = moveMesh.WorldToCell(new Vector2(100f, 100f), out _, out _);
            Assert.That(isValid, Is.False);
        }
        #endregion

        #region InitializationTests
        [Test]
        public void InitializePathfindingCache_ValidMoveMesh_MarksCacheValid()
        {
            MoveMesh moveMesh = CreateMoveMesh(5, 5, 1f, AllWalkable(5, 5));
            PathFinder pathFinder = CreatePathFinder(moveMesh);
            Assert.That(pathFinder.IsValidPathFinder(), Is.True);
        }

        // Note: For the path content assertions, only same-row start/target pairs over an open grid are used
        // - a same-row target is cheapest via pure cardinal movement (diagonal moves cost 1.41421 vs 1 for cardinal)
        // - a straight unobstructed line collapses to a single point after StringPull - so the expected result is provable by hand
        [Test]
        public void FindPath_OpenGridSameRow_ReturnsTrueAndCollapsesToSingleTargetPoint()
        {
            MoveMesh moveMesh = CreateMoveMesh(5, 5, 1f, AllWalkable(5, 5));
            PathFinder pathFinder = CreatePathFinder(moveMesh);

            Vector2 start = moveMesh.CellToWorld(0, 0);
            Vector2 target = moveMesh.CellToWorld(4, 0);
            bool found = pathFinder.FindPath(start, target);

            Assert.That(found, Is.True);
            Assert.That(pathFinder.currentPath, Is.EqualTo(new List<Vector2> { target }));
            Assert.That(pathFinder.GetNextPathTarget(), Is.EqualTo(target));
        }

        [Test]
        public void GetNextPathTarget_BeforeAnyPathFound_ReturnsZero()
        {
            MoveMesh moveMesh = CreateMoveMesh(5, 5, 1f, AllWalkable(5, 5));
            PathFinder pathFinder = CreatePathFinder(moveMesh);
            Assert.That(pathFinder.GetNextPathTarget(), Is.EqualTo(Vector2.zero));
        }
        
        [Test]
        public void InitializePathfindingCache_WithoutInjectedMoveMesh_FindsItViaRealPhysicsDiscovery()
        {
            MoveMesh moveMesh = CreateMoveMesh(5, 5, 1f, AllWalkable(5, 5));
            // RunDetection must be set up manually for the physics query below to find it at all
            var moveMeshCollider = moveMesh.GetComponent<BoxCollider2D>();
            moveMeshCollider.enabled = true;
            moveMeshCollider.size = new Vector2(10f, 10f);
            moveMeshCollider.offset = Vector2.zero;

            var pathFinder = CreateGameObject().AddComponent<PathFinder>();
            // Note:  Deliberately NOT setting pathFinder.cachedMoveMesh - exercise TryFindMoveMesh() Physics2D.OverlapCircle + layer-mask discovery path
            pathFinder.InitializePathfindingCache();

            Assert.That(pathFinder.IsValidPathFinder(), Is.True);
            Assert.That(pathFinder.cachedMoveMesh, Is.SameAs(moveMesh));
        }
        #endregion

        #region FindPathBestReachableTests
        [Test]
        public void FindPath_FullyPartitioningWall_ReturnsFalse()
        {
            // Column 2 is unwalkable across all 5 rows ~ no path can exist b/w either side
            int columns = 5, rows = 5;
            bool[] cells = AllWalkable(columns, rows);
            for (int row = 0; row < rows; row++) { cells[row * columns + 2] = false; }

            MoveMesh moveMesh = CreateMoveMesh(columns, rows, 1f, cells);
            PathFinder pathFinder = CreatePathFinder(moveMesh);

            bool found = pathFinder.FindPath(moveMesh.CellToWorld(0, 0), moveMesh.CellToWorld(4, 0));
            Assert.That(found, Is.False);
        }

        [Test]
        public void FindPath_WallWithSingleGap_ReturnsTrueAndReachesExactTargetCell()
        {
            // Column 2 is unwalkable for rows 0-3, leaving row 4 as the only gap through the wall
            int columns = 5, rows = 5;
            bool[] cells = AllWalkable(columns, rows);
            for (int row = 0; row < 4; row++) { cells[row * columns + 2] = false; }

            MoveMesh moveMesh = CreateMoveMesh(columns, rows, 1f, cells);
            PathFinder pathFinder = CreatePathFinder(moveMesh);

            Vector2 target = moveMesh.CellToWorld(4, 0);
            bool found = pathFinder.FindPath(moveMesh.CellToWorld(0, 0), target);

            Assert.That(found, Is.True);
            Assert.That(pathFinder.currentPath, Is.Not.Empty);
            Assert.That(pathFinder.currentPath.Last(), Is.EqualTo(target));
        }

        [Test]
        public void FindPath_UnwalkableStartCell_ReturnsFalse()
        {
            int columns = 5, rows = 5;
            bool[] cells = AllWalkable(columns, rows);
            cells[0] = false; // (column 0, row 0) - the very cell we'll start from

            MoveMesh moveMesh = CreateMoveMesh(columns, rows, 1f, cells);
            PathFinder pathFinder = CreatePathFinder(moveMesh);

            bool found = pathFinder.FindPath(moveMesh.CellToWorld(0, 0), moveMesh.CellToWorld(4, 0));
            Assert.That(found, Is.False);
        }
        
        [Test]
        public void FindBestReachablePosition_TargetOutOfRange_ReturnsClosestReachableCellTowardIt()
        {
            // Open 5x5 grid, standing at (2,2), an out-of-range target at (4,4), allowed to travel 1.5 cell-widths
            // (3,3) is the walkable cell within that radius closest to the target
            MoveMesh moveMesh = CreateMoveMesh(5, 5, 1f, AllWalkable(5, 5));
            PathFinder pathFinder = CreatePathFinder(moveMesh);

            Vector2 currentPosition = moveMesh.CellToWorld(2, 2);
            Vector2 targetPosition = moveMesh.CellToWorld(4, 4);
            Vector2 result = pathFinder.FindBestReachablePosition(currentPosition, targetPosition, 1.5f);

            Vector2 expected = moveMesh.CellToWorld(3, 3);
            Assert.That(result.x, Is.EqualTo(expected.x).Within(0.0001f));
            Assert.That(result.y, Is.EqualTo(expected.y).Within(0.0001f));
        }

        [Test]
        public void FindBestReachablePosition_NoValidCache_ReturnsZero()
        {
            var pathFinder = CreateGameObject().AddComponent<PathFinder>();
            Vector2 result = pathFinder.FindBestReachablePosition(Vector2.zero, Vector2.one, 5f);
            Assert.That(result, Is.EqualTo(Vector2.zero));
        }
        #endregion
    }
}
