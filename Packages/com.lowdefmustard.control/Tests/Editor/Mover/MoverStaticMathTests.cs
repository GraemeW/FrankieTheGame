using NUnit.Framework;
using UnityEngine;

namespace LowDefMustard.Control.Tests.Editor
{
    // Covered: Static pure-math helpers only
    // Not covered: MonoBehaviour lifecycle, rigidbody movement, target reckoning (covered separately)
    
    public class MoverStaticMathTests
    {
        [TestCase(0.05f, 0f)]
        [TestCase(-0.05f, 0f)]
        [TestCase(0f, 0f)]
        public void SignFloored_BelowThreshold_ReturnsZero(float input, float expected)
        {
            Assert.That(Mover.SignFloored(input), Is.EqualTo(expected));
        }

        [TestCase(0.1f, 1f)]
        [TestCase(-0.1f, -1f)]
        public void SignFloored_AtThreshold_ReturnsSign(float input, float expected)
        {
            // Threshold check is strict '<' - exactly-0.1 magnitude is NOT floored
            Assert.That(Mover.SignFloored(input), Is.EqualTo(expected));
        }

        [TestCase(0.5f, 1f)]
        [TestCase(-2.3f, -1f)]
        [TestCase(100f, 1f)]
        public void SignFloored_AboveThreshold_ReturnsSign(float input, float expected)
        {
            Assert.That(Mover.SignFloored(input), Is.EqualTo(expected));
        }

        [Test]
        public void RoundToPixelPerfect_RoundsEachAxisToNearestPixel()
        {
            // pixelsPerUnit is 100, so 0.123 -> 12.3 -> rounds to 12 -> 0.12; 0.456 -> 45.6 -> rounds to 46 -> 0.46
            Vector2 result = Mover.RoundToPixelPerfect(new Vector2(0.123f, 0.456f));
            Assert.That(result.x, Is.EqualTo(0.12f).Within(0.0001f));
            Assert.That(result.y, Is.EqualTo(0.46f).Within(0.0001f));
        }

        [Test]
        public void RoundToPixelPerfect_NegativeValues_RoundsSymmetrically()
        {
            Vector2 result = Mover.RoundToPixelPerfect(new Vector2(-0.123f, -0.456f));
            Assert.That(result.x, Is.EqualTo(-0.12f).Within(0.0001f));
            Assert.That(result.y, Is.EqualTo(-0.46f).Within(0.0001f));
        }

        [Test]
        public void RoundToPixelPerfect_ExactMidpoint_RoundsToEven()
        {
            // Mathf.Round uses banker's rounding: 12.5 -> 12 (even), 13.5 -> 14 (even)
            Vector2 result = Mover.RoundToPixelPerfect(new Vector2(0.125f, 0.135f));
            Assert.That(result.x, Is.EqualTo(0.12f).Within(0.0001f));
            Assert.That(result.y, Is.EqualTo(0.14f).Within(0.0001f));
        }
    }
}
