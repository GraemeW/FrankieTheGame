using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace LowDefMustard.Utils.Tests.Editor
{
    public class ProbabilityPairOperationTests
    {
        private class TestPair : IObjectProbabilityPair<string>
        {
            private readonly string value;
            private readonly int probability;

            public TestPair(string value, int probability)
            {
                this.value = value;
                this.probability = probability;
            }

            public string GetObject() => value;
            public int GetProbability() => probability;
        }

        [Test]
        public void GetRandomObject_SingleCandidate_AlwaysReturnsIt()
        {
            var pairs = new List<IObjectProbabilityPair<string>> { new TestPair("only", 5) };

            // No seeding needed - only one bucket exists, so the roll can't matter
            string result = ProbabilityPairOperation<string>.GetRandomObject(pairs);

            Assert.AreEqual("only", result);
        }

        [Test]
        public void GetRandomObject_ZeroProbabilityCandidate_NeverSelected()
        {
            // Structurally guaranteed, not just statistically likely
            // Zero-weight entry can never be reached by the cumulative bucket check regardless of its position in the list
            var pairs = new List<IObjectProbabilityPair<string>>
            {
                new TestPair("a", 5),
                new TestPair("never", 0),
                new TestPair("c", 5)
            };

            for (int i = 0; i < 50; i++)
            {
                string result = ProbabilityPairOperation<string>.GetRandomObject(pairs);
                Assert.AreNotEqual("never", result);
            }
        }

        [Test]
        public void GetRandomObject_EmptyList_ReturnsDefault()
        {
            // Deterministic, not probabilistic
            // probabilityDenominator is 0, so Random.Range(0, 0) returns 0 and the empty loop falls through
            var pairs = new List<IObjectProbabilityPair<string>>();

            string result = ProbabilityPairOperation<string>.GetRandomObject(pairs);

            Assert.IsNull(result);
        }

        [Test]
        public void GetRandomObject_AllZeroProbabilities_ReturnsDefault()
        {
            // Also deterministic
            // Denominator is 0, so the roll is 0 and every cumulative bucket check (0 < 0) fails
            var pairs = new List<IObjectProbabilityPair<string>>
            {
                new TestPair("a", 0),
                new TestPair("b", 0)
            };

            string result = ProbabilityPairOperation<string>.GetRandomObject(pairs);

            Assert.IsNull(result);
        }

        [Test]
        public void GetRandomObject_OverManyTrials_RoughlyRespectsWeighting()
        {
            // Statistical test, seeded for reproducibility - generous tolerance to avoid flakiness
            // Accepting some looseness here in exchange for actually exercising the weighting behaviour, not just structural edge cases
            Random.InitState(12345);
            var pairs = new List<IObjectProbabilityPair<string>>
            {
                new TestPair("common", 90),
                new TestPair("rare", 10)
            };

            int commonCount = 0;
            const int trials = 2000;
            for (int i = 0; i < trials; i++)
            {
                if (ProbabilityPairOperation<string>.GetRandomObject(pairs) == "common") { commonCount++; }
            }

            float commonRatio = (float)commonCount / trials;
            Assert.That(commonRatio, Is.InRange(0.80f, 0.98f));
        }
    }
}
