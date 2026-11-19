using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace LowDefMustard.Utils.Tests.Editor
{
    public class ProbabilityPairExtensionsTests
    {
        // Tested:
        //  - TryGetRandom (single) - one candidate, zero / negative weights, empty values (null / destroyed Unity object) keep their weight + pick nothing, empty + null input, weighting
        //  - TryGetRandom (count) - no repeats, repeated entries collapse, fewer candidates than asked, empty values never fill a pick, count of 0
        //  - IObjectProbabilityPair covariance - a derived-value pair list rolls as its base type

        #region Helpers
        private static List<IObjectProbabilityPair<string>> Pairs(params (string value, int weight)[] entries) => TestProbabilityPair<string>.ListOf(entries);
        #endregion

        #region Tests
        [Test]
        public void TryGetRandom_SingleCandidate_AlwaysReturnsIt()
        {
            // No seeding needed - only one bucket exists, so the roll can't matter
            Assert.IsTrue(Pairs(("only", 5)).TryGetRandom(out string result));
            Assert.AreEqual("only", result);
        }

        [Test]
        public void TryGetRandom_ZeroAndNegativeWeights_NeverSelected()
        {
            // Negative weight would otherwise shrink the total and skew the buckets
            List<IObjectProbabilityPair<string>> pairs = Pairs(("a", 5), ("never", 0), ("negative", -10), ("c", 5));

            for (int i = 0; i < 50; i++)
            {
                Assert.IsTrue(pairs.TryGetRandom(out string result));
                CollectionAssert.Contains(new[] { "a", "c" }, result);
            }
        }

        [Test]
        public void TryGetRandom_NullValue_KeepsItsWeightAndPicksNothing()
        {
            // e.g. a loot table's "no loot this time" entry ~ statistical, seeded, generous tolerance
            Random.InitState(12345);
            List<IObjectProbabilityPair<string>> pairs = Pairs((null, 50), ("kept", 50));

            int nothingCount = 0;
            const int trials = 2000;
            for (int i = 0; i < trials; i++)
            {
                if (!pairs.TryGetRandom(out string result))
                {
                    Assert.IsNull(result);
                    nothingCount++;
                    continue;
                }
                Assert.AreEqual("kept", result);
            }

            Assert.That((float)nothingCount / trials, Is.InRange(0.4f, 0.6f));
        }

        [Test]
        public void TryGetRandom_DestroyedUnityObject_PicksNothingInstead()
        {
            // Generic != null misses Unity's destroyed-object check, this guards the Equals(null) path
            var destroyed = ScriptableObject.CreateInstance<ScriptableObject>();
            var kept = ScriptableObject.CreateInstance<ScriptableObject>();
            Object.DestroyImmediate(destroyed);
            var pairs = new List<IObjectProbabilityPair<ScriptableObject>> { new TestProbabilityPair<ScriptableObject>(destroyed, 100), new TestProbabilityPair<ScriptableObject>(kept, 1) };

            try
            {
                bool wasNothingPicked = false;
                for (int i = 0; i < 50; i++)
                {
                    if (!pairs.TryGetRandom(out ScriptableObject result))
                    {
                        Assert.IsNull(result);
                        wasNothingPicked = true;
                        continue;
                    }
                    Assert.AreSame(kept, result, "never the destroyed object");
                }
                Assert.IsTrue(wasNothingPicked, "the destroyed entry keeps its weight (100 vs 1)");
            }
            finally { Object.DestroyImmediate(kept); }
        }

        [Test]
        public void TryGetRandom_NothingUsable_ReturnsFalseAndDefault()
        {
            Assert.IsFalse(Pairs().TryGetRandom(out string emptyResult));
            Assert.IsNull(emptyResult);
            Assert.IsFalse(Pairs(("a", 0), (null, 3)).TryGetRandom(out string unusableResult));
            Assert.IsNull(unusableResult);
            Assert.IsFalse(((List<IObjectProbabilityPair<string>>)null).TryGetRandom(out string nullResult));
            Assert.IsNull(nullResult);
        }

        [Test]
        public void TryGetRandom_OverManyTrials_RoughlyRespectsWeighting()
        {
            // Statistical test, seeded for reproducibility - generous tolerance to avoid flakiness
            Random.InitState(12345);
            List<IObjectProbabilityPair<string>> pairs = Pairs(("common", 90), ("rare", 10));

            int commonCount = 0;
            const int trials = 2000;
            for (int i = 0; i < trials; i++)
            {
                if (pairs.TryGetRandom(out string result) && result == "common") { commonCount++; }
            }

            float commonRatio = (float)commonCount / trials;
            Assert.That(commonRatio, Is.InRange(0.80f, 0.98f));
        }

        [Test]
        public void TryGetRandomCount_MoreCandidatesThanCount_PicksCountWithoutRepeats()
        {
            List<IObjectProbabilityPair<string>> pairs = Pairs(("a", 1), ("b", 1), ("c", 1), ("d", 1), ("e", 1));

            Assert.IsTrue(pairs.TryGetRandom(3, out List<string> results));

            Assert.AreEqual(3, results.Count);
            CollectionAssert.AllItemsAreUnique(results);
            CollectionAssert.IsSubsetOf(results, new[] { "a", "b", "c", "d", "e" });
        }

        [Test]
        public void TryGetRandomCount_RepeatedEntries_PickedOnce()
        {
            List<IObjectProbabilityPair<string>> pairs = Pairs(("a", 1), ("a", 1), ("b", 1));

            Assert.IsTrue(pairs.TryGetRandom(4, out List<string> results));

            CollectionAssert.AreEquivalent(new[] { "a", "b" }, results);
        }

        [Test]
        public void TryGetRandomCount_EmptyValues_NeverFillAPick()
        {
            List<IObjectProbabilityPair<string>> pairs = Pairs((null, 100), ("a", 1), ("b", 1));

            Assert.IsTrue(pairs.TryGetRandom(2, out List<string> results));

            CollectionAssert.AreEquivalent(new[] { "a", "b" }, results);
        }

        [Test]
        public void TryGetRandomCount_ZeroCountOrNothingUsable_ReturnsFalseAndEmpty()
        {
            Assert.IsFalse(Pairs(("a", 1)).TryGetRandom(0, out List<string> zeroCountResults));
            CollectionAssert.IsEmpty(zeroCountResults);
            Assert.IsFalse(Pairs(("a", 0)).TryGetRandom(2, out List<string> unusableResults));
            CollectionAssert.IsEmpty(unusableResults);
        }

        [Test]
        public void TryGetRandom_DerivedValuePairs_RollAsBaseType()
        {
            // Compile-time check as much as runtime ~ relies on IObjectProbabilityPair<out T>
            var derivedPairs = new List<TestProbabilityPair<string>> { new("only", 1) };
            IEnumerable<IObjectProbabilityPair<object>> basePairs = derivedPairs;

            Assert.IsTrue(basePairs.TryGetRandom(out object result));
            Assert.AreEqual("only", result);
        }
        #endregion
    }
}
