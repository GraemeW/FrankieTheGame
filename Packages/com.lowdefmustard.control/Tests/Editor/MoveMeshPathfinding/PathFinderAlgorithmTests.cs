using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace LowDefMustard.Control.Tests.Editor
{
    // Covered: PathFinder's static helpers
    // Not covered: FindPath/RunAStar/InitializePathfindingCache/FindBestReachablePosition - covered separately
    public class PathFinderAlgorithmTests
    {
        private static AStarNode MakeNode(float gridCost, float heuristicCost)
        {
            var node = new AStarNode();
            node.Initialize(0, 0, gridCost, heuristicCost, null);
            return node;
        }

        [Test]
        public void HeapPushPop_MaintainsMinHeapOrderByFinalCost()
        {
            var heap = new List<AStarNode>();
            float[] costs = { 5f, 1f, 4f, 2f, 3f };
            foreach (float cost in costs) { PathFinder.HeapPush(heap, MakeNode(cost, 0f)); }

            var poppedOrder = new List<float>();
            while (heap.Count > 0) { poppedOrder.Add(PathFinder.HeapPop(heap).GetFinalCost()); }

            Assert.That(poppedOrder, Is.EqualTo(new[] { 1f, 2f, 3f, 4f, 5f }));
        }

        [Test]
        public void HeapPushPop_SingleElement_PopsThatElement()
        {
            var heap = new List<AStarNode>();
            AStarNode node = MakeNode(7f, 0f);
            PathFinder.HeapPush(heap, node);
            Assert.That(PathFinder.HeapPop(heap), Is.SameAs(node));
            Assert.That(heap, Is.Empty);
        }

        [Test]
        public void HeapPushPop_TiedCosts_PopsAllTiedNodesWithoutError()
        {
            var heap = new List<AStarNode>();
            for (int i = 0; i < 4; i++) { PathFinder.HeapPush(heap, MakeNode(3f, 0f)); }

            var poppedCosts = new List<float>();
            while (heap.Count > 0) { poppedCosts.Add(PathFinder.HeapPop(heap).GetFinalCost()); }

            Assert.That(poppedCosts, Has.All.EqualTo(3f));
            Assert.That(poppedCosts, Has.Count.EqualTo(4));
        }

        [Test]
        public void DistanceEquivalentHeuristic_SamePoint_ReturnsZero()
        {
            Assert.That(PathFinder.DistanceEquivalentHeuristic(2, 2, 2, 2), Is.EqualTo(0f));
        }

        [Test]
        public void DistanceEquivalentHeuristic_PureHorizontal_ReturnsColumnDelta()
        {
            // No diagonal component, so the diagonal-discount term contributes nothing
            Assert.That(PathFinder.DistanceEquivalentHeuristic(0, 0, 5, 0), Is.EqualTo(5f).Within(0.0001f));
        }

        [Test]
        public void DistanceEquivalentHeuristic_PureDiagonal_ApproximatesDiagonalDistance()
        {
            // Equal column/row delta should approximate n * sqrt(2), matching true diagonal travel cost
            float result = PathFinder.DistanceEquivalentHeuristic(0, 0, 3, 3);
            Assert.That(result, Is.EqualTo(3f * 1.41421f).Within(0.001f));
        }

        [Test]
        public void StringPull_SingleElementPath_ReturnsUnchanged()
        {
            var path = new List<Vector2> { new(5f, 5f) };
            List<Vector2> result = PathFinder.StringPull(Vector2.zero, path, 0.0016f, 0.01f);
            Assert.That(result, Is.EqualTo(new List<Vector2> { new(5f, 5f) }));
        }

        [Test]
        public void StringPull_CollinearPath_CollapsesToFinalPointOnly()
        {
            var path = new List<Vector2> { new(1f, 0f), new(2f, 0f), new(3f, 0f) };
            List<Vector2> result = PathFinder.StringPull(Vector2.zero, path, 0.0016f, 0.01f);
            Assert.That(result, Is.EqualTo(new List<Vector2> { new(3f, 0f) }));
        }

        [Test]
        public void StringPull_PathWithATurn_PreservesTheCorner()
        {
            var path = new List<Vector2> { new(1f, 0f), new(1f, 1f) };
            List<Vector2> result = PathFinder.StringPull(Vector2.zero, path, 0.0016f, 0.01f);
            Assert.That(result, Is.EqualTo(new List<Vector2> { new(1f, 0f), new(1f, 1f) }));
        }
    }
}
