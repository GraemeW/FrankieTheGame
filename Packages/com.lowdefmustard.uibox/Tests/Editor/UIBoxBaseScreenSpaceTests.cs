using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace LowDefMustard.UIBox.Tests.Editor
{
    // Test Notes:
    //  - production code passes a null camera for a Screen Space Overlay canvas
    //      - so, WorldToScreenPoint(null, worldPoint) returns the world x/y directly
    //  - thus, bare RectTransform's position/sizeDelta fully determines its screen rect (no real canvas needed to test)
    
    public class UIBoxBaseScreenSpaceTests
    {
        // State
        private readonly List<GameObject> spawned = new();

        // Setup
        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in spawned.Where(go => go != null))
            {
                Object.DestroyImmediate(go);
            }

            spawned.Clear();
        }

        #region Private Methods
        private RectTransform CreateRect(string name, Vector2 center, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            spawned.Add(go);
            var rectTransform = (RectTransform)go.transform;
            rectTransform.sizeDelta = size;
            rectTransform.position = new Vector3(center.x, center.y, 0f);
            return rectTransform;
        }

        private UIChoiceButton CreateChoiceAt(Vector2 center, Vector2 size)
        {
            UIChoiceButton choice = TestChoiceFactory.CreateWiredButton("Choice", spawned);
            var rectTransform = (RectTransform)choice.transform;
            rectTransform.sizeDelta = size;
            rectTransform.position = new Vector3(center.x, center.y, 0f);
            return choice;
        }
        #endregion

        #region TryGetScreenRect
        [Test]
        public void TryGetScreenRect_NullRectTransform_ReturnsFalse()
        {
            bool result = UIBoxBase.TryGetScreenRect(null, null, out _);

            Assert.IsFalse(result);
        }

        [Test]
        public void TryGetScreenRect_NullCamera_UsesWorldPositionDirectlyAsScreenPosition()
        {
            RectTransform rectTransform = CreateRect("Rect", new Vector2(100f, 50f), new Vector2(20f, 10f));

            bool result = UIBoxBase.TryGetScreenRect(null, rectTransform, out Rect screenRect);

            Assert.IsTrue(result);
            Assert.AreEqual(new Rect(90f, 45f, 20f, 10f), screenRect);
        }
        #endregion

        #region TryFindClosestRayHit
        [Test]
        public void TryFindClosestRayHit_PicksNearestRectAlongRay_IgnoringFartherOnes()
        {
            UIChoiceButton near = CreateChoiceAt(new Vector2(10f, 0f), new Vector2(4f, 4f));
            UIChoiceButton far = CreateChoiceAt(new Vector2(30f, 0f), new Vector2(4f, 4f));
            var options = new List<UIChoice> { near, far };

            bool hit = UIBoxBase.TryFindClosestRayHit(Vector2.zero, Vector2.right, null, options, null, out UIChoice closest);

            Assert.IsTrue(hit);
            Assert.AreSame(near, closest);
        }

        [Test]
        public void TryFindClosestRayHit_SkipsHighlightedAndNullEntries()
        {
            UIChoiceButton highlighted = CreateChoiceAt(new Vector2(10f, 0f), new Vector2(4f, 4f));
            UIChoiceButton next = CreateChoiceAt(new Vector2(20f, 0f), new Vector2(4f, 4f));
            var options = new List<UIChoice> { highlighted, null, next };

            bool hit = UIBoxBase.TryFindClosestRayHit(Vector2.zero, Vector2.right, null, options, highlighted, out UIChoice closest);

            Assert.IsTrue(hit);
            Assert.AreSame(next, closest);
        }

        [Test]
        public void TryFindClosestRayHit_NoneOnRay_ReturnsFalse()
        {
            UIChoiceButton offToTheSide = CreateChoiceAt(new Vector2(0f, 50f), new Vector2(4f, 4f));
            var options = new List<UIChoice> { offToTheSide };

            bool hit = UIBoxBase.TryFindClosestRayHit(Vector2.zero, Vector2.right, null, options, null, out UIChoice closest);

            Assert.IsFalse(hit);
            Assert.IsNull(closest);
        }
        #endregion

        #region TryFindBestAngleMatch
        [Test]
        public void TryFindBestAngleMatch_RejectsCandidatesOutsideEightyFiveDegreeCone()
        {
            UIChoiceButton perpendicular = CreateChoiceAt(new Vector2(0f, 10f), new Vector2(1f, 1f));
            var options = new List<UIChoice> { perpendicular };

            bool found = UIBoxBase.TryFindBestAngleMatch(Vector2.zero, Vector2.right, null, options, null, out _);

            Assert.IsFalse(found);
        }

        [Test]
        public void TryFindBestAngleMatch_WithinCone_PicksHigherScoringCloserCandidate()
        {
            // Both directly ahead, closer candidate scores higher (1/distance)
            UIChoiceButton closeAhead = CreateChoiceAt(new Vector2(10f, 0f), new Vector2(1f, 1f));
            UIChoiceButton farAhead = CreateChoiceAt(new Vector2(30f, 0f), new Vector2(1f, 1f));
            var options = new List<UIChoice> { farAhead, closeAhead };

            bool found = UIBoxBase.TryFindBestAngleMatch(Vector2.zero, Vector2.right, null, options, null, out UIChoice best);

            Assert.IsTrue(found);
            Assert.AreSame(closeAhead, best);
        }

        [Test]
        public void TryFindBestAngleMatch_SkipsHighlightedAndNullEntries()
        {
            UIChoiceButton highlighted = CreateChoiceAt(new Vector2(10f, 0f), new Vector2(1f, 1f));
            UIChoiceButton next = CreateChoiceAt(new Vector2(20f, 0f), new Vector2(1f, 1f));
            var options = new List<UIChoice> { highlighted, null, next};

            bool found = UIBoxBase.TryFindBestAngleMatch(Vector2.zero, Vector2.right, null, options, highlighted, out UIChoice closest);

            Assert.IsTrue(found);
            Assert.AreSame(next, closest);
        }
        #endregion
    }
}
