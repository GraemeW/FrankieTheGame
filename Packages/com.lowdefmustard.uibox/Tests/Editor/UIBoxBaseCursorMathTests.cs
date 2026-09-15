using LowDefMustard.Control;
using NUnit.Framework;
using UnityEngine;

namespace LowDefMustard.UIBox.Tests.Editor
{
    public class UIBoxBaseCursorMathTests
    {
        #region TryExecuteMove
        [TestCase(ControllerInputType.NavigateRight, 0, 5, CursorMovementStyle.Combined, 1)]
        [TestCase(ControllerInputType.NavigateRight, 4, 5, CursorMovementStyle.Combined, 0)]
        [TestCase(ControllerInputType.NavigateLeft, 0, 5, CursorMovementStyle.Combined, 4)]
        [TestCase(ControllerInputType.NavigateLeft, 4, 5, CursorMovementStyle.Combined, 3)]
        [TestCase(ControllerInputType.NavigateDown, 0, 5, CursorMovementStyle.Combined, 1)]
        [TestCase(ControllerInputType.NavigateUp, 0, 5, CursorMovementStyle.Combined, 4)]
        public void TryExecuteMove_LinearWraparound_MatchesExpected(ControllerInputType inputType, int startIndex, int optionsCount, CursorMovementStyle style, int expectedIndex)
        {
            int index = startIndex;
            bool valid = UIBoxBase.TryExecuteMove(inputType, ref index, optionsCount, style);

            Assert.IsTrue(valid);
            Assert.AreEqual(expectedIndex, index);
        }

        [TestCase(ControllerInputType.NavigateRight, CursorMovementStyle.Vertical)]
        [TestCase(ControllerInputType.NavigateDown, CursorMovementStyle.Horizontal)]
        public void TryExecuteMove_InputFilteredOutByStyle_ReturnsFalseAndLeavesIndexUnchanged(ControllerInputType inputType, CursorMovementStyle style)
        {
            int index = 2;
            bool valid = UIBoxBase.TryExecuteMove(inputType, ref index, 4, style);

            Assert.IsFalse(valid);
            Assert.AreEqual(2, index);
        }

        [Test]
        public void TryExecuteMove_UnhandledInputType_ReturnsFalse()
        {
            int index = 1;
            bool valid = UIBoxBase.TryExecuteMove(ControllerInputType.Execute, ref index, 4, CursorMovementStyle.Combined);

            Assert.IsFalse(valid);
        }
        #endregion

        #region TryExecuteMove2D
        [TestCase(ControllerInputType.NavigateRight, 0, 5, 1)]
        [TestCase(ControllerInputType.NavigateLeft, 0, 5, 4)]
        [TestCase(ControllerInputType.NavigateDown, 3, 5, 0)] // 3 + 2 >= 5 -- wraps
        [TestCase(ControllerInputType.NavigateDown, 0, 5, 2)]
        [TestCase(ControllerInputType.NavigateUp, 3, 5, 1)]
        [TestCase(ControllerInputType.NavigateUp, 0, 5, 4)] // 0 <= 1 -- wraps
        public void TryExecuteMove2D_FixedTwoColumnWraparound_MatchesExpected(ControllerInputType inputType, int startIndex, int optionsCount, int expectedIndex)
        {
            int index = startIndex;
            bool valid = UIBoxBase.TryExecuteMove2D(inputType, ref index, optionsCount);

            Assert.IsTrue(valid);
            Assert.AreEqual(expectedIndex, index);
        }

        [Test]
        public void TryExecuteMove2D_SingleOption_AlwaysSnapsToZero()
        {
            int index = 0;
            bool valid = UIBoxBase.TryExecuteMove2D(ControllerInputType.NavigateUp, ref index, 1);

            Assert.IsTrue(valid);
            Assert.AreEqual(0, index);
        }
        #endregion

        #region TryRayIntersectsRect
        [Test]
        public void TryRayIntersectsRect_AxisAlignedHit_ReturnsDistanceToNearEdge()
        {
            var rect = Rect.MinMaxRect(5f, -2f, 10f, 2f);
            bool hit = UIBoxBase.TryRayIntersectsRect(Vector2.zero, Vector2.right, rect, out float distance);

            Assert.IsTrue(hit);
            Assert.AreEqual(5f, distance, 0.0001f);
        }

        [Test]
        public void TryRayIntersectsRect_RectBehindOrigin_ReturnsFalse()
        {
            var rect = Rect.MinMaxRect(-10f, -2f, -5f, 2f);
            bool hit = UIBoxBase.TryRayIntersectsRect(Vector2.zero, Vector2.right, rect, out _);

            Assert.IsFalse(hit);
        }

        [Test]
        public void TryRayIntersectsRect_OriginInsideRect_ReturnsExitDistanceNotEntryDistance()
        {
            var rect = Rect.MinMaxRect(-5f, -5f, 5f, 5f);
            bool hit = UIBoxBase.TryRayIntersectsRect(Vector2.zero, Vector2.right, rect, out float distance);

            // Entry distance (tMin) is negative since the origin is already inside the rect, falls back to the exit distance (tMax)
            Assert.IsTrue(hit);
            Assert.AreEqual(5f, distance, 0.0001f);
        }

        [Test]
        public void TryRayIntersectsRect_DiagonalHit_ReturnsEuclideanDistanceToCorner()
        {
            var rect = Rect.MinMaxRect(5f, 5f, 10f, 10f);
            var direction = new Vector2(1f, 1f).normalized;
            bool hit = UIBoxBase.TryRayIntersectsRect(Vector2.zero, direction, rect, out float distance);

            Assert.IsTrue(hit);
            Assert.AreEqual(Mathf.Sqrt(50f), distance, 0.001f);
        }

        [Test]
        public void TryRayIntersectsRect_RayParallelAndOffAxis_ReturnsFalse()
        {
            // Ray travels along y=0, missing the rect's y-range - axis-independent early-out (each axis checked separately)
            var rect = Rect.MinMaxRect(5f, 5f, 10f, 10f);
            bool hit = UIBoxBase.TryRayIntersectsRect(Vector2.zero, Vector2.right, rect, out _);

            Assert.IsFalse(hit);
        }
        #endregion
    }
}
