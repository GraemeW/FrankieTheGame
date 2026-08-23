using NUnit.Framework;

namespace LowDefMustard.Utils.Tests.Editor
{
    public class ApproximateFloatComparerTests
    {
        [Test]
        public void Equals_ValuesWithinDefaultTolerance_ReturnsTrue()
        {
            var comparer = new ApproximateFloatComparer();

            Assert.IsTrue(comparer.Equals(1.000f, 1.002f));
        }

        [Test]
        public void Equals_ValuesOutsideDefaultTolerance_ReturnsFalse()
        {
            var comparer = new ApproximateFloatComparer();

            Assert.IsFalse(comparer.Equals(1.000f, 1.100f));
        }

        [Test]
        public void Equals_CustomTolerance_IsRespected()
        {
            var looseComparer = new ApproximateFloatComparer(tolerance: 0.5f);

            // ApproximateFloatComparer quantizes to the nearest multiple of tolerance
            // rather than comparing raw distance, so values need to land in the same
            // bucket, not just be "within tolerance" of each other
            Assert.IsTrue(looseComparer.Equals(1.0f, 1.1f));
        }

        [TestCase(0f)]
        [TestCase(-0.01f)]
        public void Constructor_NonPositiveTolerance_FallsBackToDefault(float invalidTolerance)
        {
            var comparer = new ApproximateFloatComparer(invalidTolerance);

            // Default tolerance is 0.005f. 1.000 is a bucket centre
            // (1.000 / 0.005 = 200.0 exactly), so keep the offset comfortably inside
            // half a bucket width (0.0025) to avoid a boundary rounding flip.
            Assert.IsTrue(comparer.Equals(1.000f, 1.001f));
        }

        [Test]
        public void Equals_ValuesNearBucketBoundary_CanDifferEvenWithinTolerance()
        {
            // Documents the quantization behaviour directly: 1.000 and 1.004 are only
            // 0.004 apart (less than the 0.005 tolerance), but they straddle a bucket
            // boundary (1.004 / 0.005 = 200.8, which rounds up to the next bucket), so
            // this comparer treats them as NOT equal. Consumers relying on "distance
            // less than tolerance" semantics should be aware this isn't quite that.
            var comparer = new ApproximateFloatComparer();

            Assert.IsFalse(comparer.Equals(1.000f, 1.004f));
        }

        [Test]
        public void GetHashCode_ForValuesConsideredEqual_AreEqual()
        {
            var comparer = new ApproximateFloatComparer();

            // Contract requirement: if Equals(a, b) is true, hash codes must match too,
            // or this comparer breaks silently when used as a Dictionary/HashSet key.
            Assert.AreEqual(comparer.GetHashCode(1.000f), comparer.GetHashCode(1.002f));
        }

        [Test]
        public void Equals_BothNaN_TreatedAsEqual()
        {
            // Documents current behaviour: NaN always quantizes to 0, so two NaNs
            // compare equal under this comparer even though float.NaN != float.NaN
            // by IEEE semantics. Worth confirming this is the intended contract.
            var comparer = new ApproximateFloatComparer();

            Assert.IsTrue(comparer.Equals(float.NaN, float.NaN));
        }

        [Test]
        public void Equals_PositiveInfinityToItself_ReturnsTrue()
        {
            var comparer = new ApproximateFloatComparer();

            Assert.IsTrue(comparer.Equals(float.PositiveInfinity, float.PositiveInfinity));
        }

        [Test]
        public void Equals_PositiveAndNegativeInfinity_ReturnsFalse()
        {
            var comparer = new ApproximateFloatComparer();

            Assert.IsFalse(comparer.Equals(float.PositiveInfinity, float.NegativeInfinity));
        }
    }
}
