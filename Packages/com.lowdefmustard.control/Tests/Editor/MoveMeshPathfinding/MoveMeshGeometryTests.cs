using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace LowDefMustard.Control.Tests.Editor
{
    // Covered: MoveMesh's pure-logic static helpers 
    // Not covered: RunDetection's pipeline (CompositeCollider2D scanning, RasteriseAdditionalColliders, gizmo mesh baking) - covered separately
    
    public class MoveMeshGeometryTests
    {
        // ComputeSignedArea / EnsureCounterClockwise
        // N.B. Sign convention here is whatever this shoelace variant produces,
        //  -> not a claim about "true" CW/CCW ~ these tests just pin down the two possible outcomes for a simple square

        [Test]
        public void ComputeSignedArea_UnitSquare_NegativeWinding_ReturnsNegative()
        {
            var square = new List<Vector2> { new(0, 0), new(1, 0), new(1, 1), new(0, 1) };
            Assert.That(MoveMesh.ComputeSignedArea(square), Is.EqualTo(-1f).Within(0.0001f));
        }

        [Test]
        public void ComputeSignedArea_UnitSquare_OppositeWinding_ReturnsPositive()
        {
            var square = new List<Vector2> { new(0, 0), new(0, 1), new(1, 1), new(1, 0) };
            Assert.That(MoveMesh.ComputeSignedArea(square), Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void EnsureCounterClockwise_NegativeAreaWinding_ReversesPoints()
        {
            var square = new List<Vector2> { new(0, 0), new(1, 0), new(1, 1), new(0, 1) };
            List<Vector2> result = MoveMesh.EnsureCounterClockwise(square);
            Assert.That(result, Is.EqualTo(new List<Vector2> { new(0, 1), new(1, 1), new(1, 0), new(0, 0) }));
        }

        [Test]
        public void EnsureCounterClockwise_PositiveAreaWinding_LeavesPointsUnchanged()
        {
            var square = new List<Vector2> { new(0, 0), new(0, 1), new(1, 1), new(1, 0) };
            List<Vector2> result = MoveMesh.EnsureCounterClockwise(square);
            Assert.That(result, Is.EqualTo(square));
        }

        // FloodFillOutside / BuildEnclosedGrid
        [Test]
        public void FloodFillOutside_AllOpenGrid_EverythingReachableFromBorder()
        {
            bool[] grid = new bool[5 * 5];
            bool[] outside = MoveMesh.FloodFillOutside(grid, 5, 5);
            Assert.That(outside, Has.All.True);
        }

        [Test]
        public void FloodFillOutside_RingedOffCenterCell_IsNotMarkedOutside()
        {
            // Build a 5x5 grid with an obstacle ring around the centre cell, sealing it off from the border
            bool[] grid = new bool[5 * 5];
            for (int c = 1; c <= 3; c++)
            {
                for (int r = 1; r <= 3; r++)
                {
                    if (c == 2 && r == 2) { continue; }
                    grid[r * 5 + c] = true;
                }
            }
            bool[] outside = MoveMesh.FloodFillOutside(grid, 5, 5);
            Assert.That(outside[2 * 5 + 2], Is.False);
            Assert.That(outside[0], Is.True);
        }

        [Test]
        public void BuildEnclosedGrid_SealedCell_IsEnclosed()
        {
            bool[] grid = new bool[5 * 5];
            for (int c = 1; c <= 3; c++)
            {
                for (int r = 1; r <= 3; r++)
                {
                    if (c == 2 && r == 2) { continue; }
                    grid[r * 5 + c] = true;
                }
            }
            bool[] outside = MoveMesh.FloodFillOutside(grid, 5, 5);
            bool[] enclosed = MoveMesh.BuildEnclosedGrid(grid, outside, 5, 5);

            Assert.That(enclosed[2 * 5 + 2], Is.True, "sealed-off open cell should be enclosed");
            Assert.That(enclosed[1 * 5 + 1], Is.False, "an obstacle cell itself is never enclosed");
            Assert.That(enclosed[0], Is.False, "a border-reachable open cell is not enclosed");
        }

        // SimplifyPolygon / PointToSegmentDist

        [Test]
        public void SimplifyPolygon_FewerThanThreePoints_ReturnsUnchanged()
        {
            var points = new List<Vector2> { new(0, 0), new(1, 1) };
            Assert.That(MoveMesh.SimplifyPolygon(points, 0.01f), Is.EqualTo(points));
        }

        [Test]
        public void SimplifyPolygon_CollinearPoints_CollapsesToEndpoints()
        {
            var points = new List<Vector2> { new(0, 0), new(1, 0), new(2, 0), new(3, 0) };
            List<Vector2> result = MoveMesh.SimplifyPolygon(points, 0.01f);
            Assert.That(result, Is.EqualTo(new List<Vector2> { new(0, 0), new(3, 0) }));
        }

        [Test]
        public void SimplifyPolygon_LShape_PreservesTheCorner()
        {
            var points = new List<Vector2> { new(0, 0), new(1, 0), new(2, 0), new(2, 1), new(2, 2) };
            List<Vector2> result = MoveMesh.SimplifyPolygon(points, 0.01f);
            Assert.That(result, Is.EqualTo(new List<Vector2> { new(0, 0), new(2, 0), new(2, 2) }));
        }

        [Test]
        public void PointToSegmentDist_PerpendicularToMidSegment_ReturnsPerpendicularDistance()
        {
            float distance = MoveMesh.PointToSegmentDist(new Vector2(5, 5), new Vector2(0, 0), new Vector2(10, 0));
            Assert.That(distance, Is.EqualTo(5f).Within(0.0001f));
        }

        [Test]
        public void PointToSegmentDist_PastSegmentEnd_ClampsToEndpoint()
        {
            float distance = MoveMesh.PointToSegmentDist(new Vector2(15, 0), new Vector2(0, 0), new Vector2(10, 0));
            Assert.That(distance, Is.EqualTo(5f).Within(0.0001f));
        }

        [Test]
        public void PointToSegmentDist_BeforeSegmentStart_ClampsToStart()
        {
            float distance = MoveMesh.PointToSegmentDist(new Vector2(-5, 0), new Vector2(0, 0), new Vector2(10, 0));
            Assert.That(distance, Is.EqualTo(5f).Within(0.0001f));
        }

        // DirectionalDelta

        [TestCase(0, 1, 0)]
        [TestCase(1, 0, 1)]
        [TestCase(2, -1, 0)]
        [TestCase(3, 0, -1)]
        public void DirectionalDelta_ReturnsExpectedOffset(int direction, int expectedDx, int expectedDy)
        {
            (int dx, int dy) = MoveMesh.DirectionalDelta(direction);
            Assert.That(dx, Is.EqualTo(expectedDx));
            Assert.That(dy, Is.EqualTo(expectedDy));
        }

        // GetScanlineSpans / SubtractSpans

        [Test]
        public void GetScanlineSpans_SquareCrossedThroughMiddle_ReturnsSingleSpan()
        {
            var square = new List<Vector2> { new(0, 0), new(4, 0), new(4, 4), new(0, 4) };
            List<(float left, float right)> spans = MoveMesh.GetScanlineSpans(square, 2f);
            Assert.That(spans, Has.Count.EqualTo(1));
            Assert.That(spans[0].left, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(spans[0].right, Is.EqualTo(4f).Within(0.0001f));
        }

        [Test]
        public void SubtractSpans_CarveInMiddle_SplitsIntoTwoSpans()
        {
            var fill = new List<(float, float)> { (0f, 10f) };
            var carve = new List<(float, float)> { (3f, 5f) };
            List<(float left, float right)> result = MoveMesh.SubtractSpans(fill, carve);
            Assert.That(result, Is.EqualTo(new List<(float, float)> { (0f, 3f), (5f, 10f) }));
        }

        [Test]
        public void SubtractSpans_CarveOutsideFill_LeavesFillUnchanged()
        {
            var fill = new List<(float, float)> { (0f, 10f) };
            var carve = new List<(float, float)> { (20f, 25f) };
            List<(float left, float right)> result = MoveMesh.SubtractSpans(fill, carve);
            Assert.That(result, Is.EqualTo(fill));
        }

        [Test]
        public void SubtractSpans_CarveFullyCoversFill_ReturnsEmpty()
        {
            var fill = new List<(float, float)> { (0f, 10f) };
            var carve = new List<(float, float)> { (-5f, 15f) };
            List<(float left, float right)> result = MoveMesh.SubtractSpans(fill, carve);
            Assert.That(result, Is.Empty);
        }

        // BakeErodedGrid

        [Test]
        public void BakeErodedGrid_ZeroErosionRadius_ReturnsCellsUnchanged()
        {
            var grid = new WalkabilityGrid { columns = 2, rows = 2, cellSize = 1f, cells = new List<bool> { true, true, true, true } };
            bool[] eroded = MoveMesh.BakeErodedGrid(grid, 0f);
            Assert.That(eroded, Is.EqualTo(new[] { true, true, true, true }));
        }

        [Test]
        public void BakeErodedGrid_AllWalkable5X5_ErodesBorderKeepsInterior()
        {
            bool[] cells = new bool[25];
            for (int i = 0; i < cells.Length; i++) { cells[i] = true; }
            var grid = new WalkabilityGrid { columns = 5, rows = 5, cellSize = 1f, cells = new List<bool>(cells) };

            bool[] eroded = MoveMesh.BakeErodedGrid(grid, 1f);

            Assert.That(eroded[2 * 5 + 2], Is.True, "grid center should survive erosion");
            Assert.That(eroded[0], Is.False, "grid corner should erode away");
            Assert.That(eroded[2 * 5 + 0], Is.False, "grid edge should erode away");
        }

        // BakeTraversalCosts

        [Test]
        public void BakeTraversalCosts_ZeroPenalty_ReturnsAllZero()
        {
            var grid = new WalkabilityGrid { columns = 2, rows = 2, cells = new List<bool> { true, true, true, true } };
            float[] costs = MoveMesh.BakeTraversalCosts(grid, 0f, 1.5f);
            Assert.That(costs, Is.EqualTo(new[] { 0f, 0f, 0f, 0f }));
        }

        [Test]
        public void BakeTraversalCosts_NoObstacles_AllCellsAtBaselineCost()
        {
            // With no unwalkable cells to seed the BFS, every walkable cell normalizes to cost 1 (no edge penalty applies)
            var grid = new WalkabilityGrid { columns = 3, rows = 3, cells = new List<bool> { true, true, true, true, true, true, true, true, true } };
            float[] costs = MoveMesh.BakeTraversalCosts(grid, 2f, 1.5f);
            Assert.That(costs, Has.All.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void BakeTraversalCosts_UnwalkableCell_ReturnsInfinity()
        {
            var grid = new WalkabilityGrid { columns = 3, rows = 3, cells = new List<bool> { false, true, true, true, true, true, true, true, true } };
            float[] costs = MoveMesh.BakeTraversalCosts(grid, 2f, 1.5f);
            Assert.That(costs[0], Is.EqualTo(Mathf.Infinity));
        }

        [Test]
        public void BakeTraversalCosts_CellNearObstacle_CostsMoreThanCellFarFromIt()
        {
            var grid = new WalkabilityGrid { columns = 3, rows = 3, cells = new List<bool> { false, true, true, true, true, true, true, true, true } };
            float[] costs = MoveMesh.BakeTraversalCosts(grid, 2f, 1.5f);
            // Index 1 (adjacent to the obstacle at index 0) should cost strictly more than index 8 (the farthest corner)
            Assert.That(costs[1], Is.GreaterThan(costs[8]));
            Assert.That(costs[8], Is.EqualTo(1f).Within(0.0001f));
        }
    }
}
