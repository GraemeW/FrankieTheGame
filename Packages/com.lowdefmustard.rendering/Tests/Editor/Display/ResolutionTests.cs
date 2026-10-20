using NUnit.Framework;
using UnityEngine;

namespace LowDefMustard.Rendering.Tests.Editor
{
    public class ResolutionTests
    {
        [Test]
        public void ResolutionScaler_Constructor_KeepsRatio()
        {
            var resolutionScaler = new ResolutionScaler(4, 3);

            Assert.AreEqual(4, resolutionScaler.numerator);
            Assert.AreEqual(3, resolutionScaler.denominator);
        }

        [Test]
        public void ResolutionScaler_CopyConstructor_CopiesRatio()
        {
            var copy = new ResolutionScaler(new ResolutionScaler(4, 3));

            Assert.AreEqual(4, copy.numerator);
            Assert.AreEqual(3, copy.denominator);
        }

        [Test]
        public void ResolutionSetting_Constructor_KeepsModeAndSize()
        {
            var resolutionSetting = new ResolutionSetting(FullScreenMode.FullScreenWindow, 1920, 1080);

            Assert.AreEqual(FullScreenMode.FullScreenWindow, resolutionSetting.fullScreenMode);
            Assert.AreEqual(1920, resolutionSetting.width);
            Assert.AreEqual(1080, resolutionSetting.height);
        }
    }
}
