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
            
            // 1.0 is a bucket centre - (1.0 / 0.5 = 2.0), so 1.1 is safely within the same bucket
            Assert.IsTrue(looseComparer.Equals(1.0f, 1.1f));
        }

        [TestCase(0f)]
        [TestCase(-0.01f)]
        public void Constructor_NonPositiveTolerance_FallsBackToDefault(float invalidTolerance)
        {
            var comparer = new ApproximateFloatComparer(invalidTolerance);

            // Default tolerance is 0.005f. 1.000 is a bucket centre - keep the offset comfortably inside half a bucket width (0.0025) to avoid a boundary rounding flip.
            Assert.IsTrue(comparer.Equals(1.000f, 1.001f));
        }

        [Test]
        public void Equals_ValuesNearBucketBoundary_CanDifferEvenWithinTolerance()
        {
            // Documents quantization behaviour directly: 1.000 and 1.004 are only 0.004 apart (less than the 0.005 tolerance), but they straddle a bucket boundary 
            // This comparer treats them as NOT equal - consumers relying on "distance less than tolerance" semantics should be aware
            var comparer = new ApproximateFloatComparer();

            Assert.IsFalse(comparer.Equals(1.000f, 1.004f));
        }

        [Test]
        public void GetHashCode_ForValuesConsideredEqual_AreEqual()
        {
            var comparer = new ApproximateFloatComparer();

            // Contract requirement: if Equals(a, b) is true, hash codes must match too, or this comparer breaks silently when used as a Dictionary/HashSet key
            Assert.AreEqual(comparer.GetHashCode(1.000f), comparer.GetHashCode(1.002f));
        }

        [Test]
        public void Equals_BothNaN_TreatedAsEqual()
        {
            // Documents current behaviour: NaN always quantizes to 0, so two NaNs compare equal under this comparer even though float.NaN != float.NaN by IEEE semantics
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
