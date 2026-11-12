using NUnit.Framework;

namespace LowDefMustard.RuleTiles.Tests.Editor
{
    public class TileAnimationSpeedTests
    {
        // Tested:
        //  - Resolve - fixed speed returned exactly across repeated calls, speed range stays within range

        #region Tests
        [TestCase(1.5f)]
        [TestCase(0.75f)]
        public void Resolve_FixedSpeed_ReturnsExactSpeed(float speed)
        {
            // Random.Range(1.5f, 1.5f) returns 1.50000012 ~8% of the time; 200 calls should guarantee a hit without the fixed-speed path
            for (int i = 0; i < 200; i++)
            {
                Assert.AreEqual(speed, TileAnimationSpeed.Resolve(speed, speed));
            }
        }

        [Test]
        public void Resolve_SpeedRange_ReturnsValueWithinRange()
        {
            for (int i = 0; i < 200; i++)
            {
                float resolved = TileAnimationSpeed.Resolve(1f, 3f);
                Assert.GreaterOrEqual(resolved, 1f);
                Assert.LessOrEqual(resolved, 3f);
            }
        }
        #endregion
    }
}
