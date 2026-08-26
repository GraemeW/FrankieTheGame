using NUnit.Framework;
using UnityEngine;

namespace LowDefMustard.Utils.Tests.Editor
{
    public class SmartVector2Tests
    {
        [Test]
        public void CheckDistance_WithinThreshold_ReturnsTrue()
        {
            var a = new Vector2(0, 0);
            var b = new Vector2(1, 0);

            Assert.IsTrue(SmartVector2.CheckDistance(a, b, distanceThreshold: 2f));
        }

        [Test]
        public void CheckDistance_BeyondThreshold_ReturnsFalse()
        {
            var a = new Vector2(0, 0);
            var b = new Vector2(5, 0);

            Assert.IsFalse(SmartVector2.CheckDistance(a, b, distanceThreshold: 2f));
        }

        [Test]
        public void CheckDistance_ExactlyAtThreshold_ReturnsFalse()
        {
            // Comparison is strictly-less-than, so distance == threshold should not count
            var a = new Vector2(0, 0);
            var b = new Vector2(2, 0);

            Assert.IsFalse(SmartVector2.CheckDistance(a, b, distanceThreshold: 2f));
        }

        [Test]
        public void CheckDistance_OutParam_ReturnsSquareMagnitude()
        {
            var a = new Vector2(0, 0);
            var b = new Vector2(3, 4); // 3-4-5 triangle - distance 5, square distance 25

            SmartVector2.CheckDistance(a, b, distanceThreshold: 100f, out float squareMagnitudeDelta);

            Assert.AreEqual(25f, squareMagnitudeDelta, 0.0001f);
        }
    }
}
