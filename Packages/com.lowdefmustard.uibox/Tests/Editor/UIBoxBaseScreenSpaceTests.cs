using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace LowDefMustard.UIBox.Tests.Editor
{
    // Test Notes:
    //  - spatial cursor movement only needs each choice's navigation rect (IUIChoice.TryGetNavigationRect)
    //  - thus, plain TestChoices with a given rect stand in for laid-out entries (no panel needed to test)

    public class UIBoxBaseScreenSpaceTests
    {
        #region TryFindClosestRayHit
        [Test]
        public void TryFindClosestRayHit_PicksNearestRectAlongRay_IgnoringFartherOnes()
        {
            var near = new TestChoice(new Vector2(10f, 0f), new Vector2(4f, 4f));
            var far = new TestChoice(new Vector2(30f, 0f), new Vector2(4f, 4f));
            var options = new List<IUIChoice> { near, far };

            bool hit = UIBoxBase.TryFindClosestRayHit(Vector2.zero, Vector2.right, options, null, out IUIChoice closest);

            Assert.IsTrue(hit);
            Assert.AreSame(near, closest);
        }

        [Test]
        public void TryFindClosestRayHit_SkipsHighlightedAndNullEntries()
        {
            var highlighted = new TestChoice(new Vector2(10f, 0f), new Vector2(4f, 4f));
            var next = new TestChoice(new Vector2(20f, 0f), new Vector2(4f, 4f));
            var options = new List<IUIChoice> { highlighted, null, next };

            bool hit = UIBoxBase.TryFindClosestRayHit(Vector2.zero, Vector2.right, options, highlighted, out IUIChoice closest);

            Assert.IsTrue(hit);
            Assert.AreSame(next, closest);
        }

        [Test]
        public void TryFindClosestRayHit_SkipsDeadAndUnplacedEntries()
        {
            var dead = new TestChoice(new Vector2(10f, 0f), new Vector2(4f, 4f)) { isAlive = false };
            var unplaced = new TestChoice(); // No navigation rect (e.g. not laid out yet)
            var next = new TestChoice(new Vector2(20f, 0f), new Vector2(4f, 4f));
            var options = new List<IUIChoice> { dead, unplaced, next };

            bool hit = UIBoxBase.TryFindClosestRayHit(Vector2.zero, Vector2.right, options, null, out IUIChoice closest);

            Assert.IsTrue(hit);
            Assert.AreSame(next, closest);
        }

        [Test]
        public void TryFindClosestRayHit_NoneOnRay_ReturnsFalse()
        {
            var offToTheSide = new TestChoice(new Vector2(0f, 50f), new Vector2(4f, 4f));
            var options = new List<IUIChoice> { offToTheSide };

            bool hit = UIBoxBase.TryFindClosestRayHit(Vector2.zero, Vector2.right, options, null, out IUIChoice closest);

            Assert.IsFalse(hit);
            Assert.IsNull(closest);
        }
        #endregion

        #region TryFindBestAngleMatch
        [Test]
        public void TryFindBestAngleMatch_RejectsCandidatesOutsideEightyFiveDegreeCone()
        {
            var perpendicular = new TestChoice(new Vector2(0f, 10f), new Vector2(1f, 1f));
            var options = new List<IUIChoice> { perpendicular };

            bool found = UIBoxBase.TryFindBestAngleMatch(Vector2.zero, Vector2.right, options, null, out _);

            Assert.IsFalse(found);
        }

        [Test]
        public void TryFindBestAngleMatch_WithinCone_PicksHigherScoringCloserCandidate()
        {
            // Both directly ahead, closer candidate scores higher (1/distance)
            var closeAhead = new TestChoice(new Vector2(10f, 0f), new Vector2(1f, 1f));
            var farAhead = new TestChoice(new Vector2(30f, 0f), new Vector2(1f, 1f));
            var options = new List<IUIChoice> { farAhead, closeAhead };

            bool found = UIBoxBase.TryFindBestAngleMatch(Vector2.zero, Vector2.right, options, null, out IUIChoice best);

            Assert.IsTrue(found);
            Assert.AreSame(closeAhead, best);
        }

        [Test]
        public void TryFindBestAngleMatch_SkipsHighlightedAndNullEntries()
        {
            var highlighted = new TestChoice(new Vector2(10f, 0f), new Vector2(1f, 1f));
            var next = new TestChoice(new Vector2(20f, 0f), new Vector2(1f, 1f));
            var options = new List<IUIChoice> { highlighted, null, next };

            bool found = UIBoxBase.TryFindBestAngleMatch(Vector2.zero, Vector2.right, options, highlighted, out IUIChoice closest);

            Assert.IsTrue(found);
            Assert.AreSame(next, closest);
        }
        #endregion
    }
}
