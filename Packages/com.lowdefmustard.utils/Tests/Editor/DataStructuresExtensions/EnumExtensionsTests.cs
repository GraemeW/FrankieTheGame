using NUnit.Framework;

namespace LowDefMustard.Utils.Tests.Editor
{
    public class EnumExtensionsTests
    {
        private enum SampleEnum
        {
            First,
            Second,
            Third
        }

        [Test]
        public void NextClamped_MiddleValue_ReturnsNextValue()
        {
            Assert.AreEqual(SampleEnum.Second, SampleEnum.First.NextClamped());
        }

        [Test]
        public void NextClamped_LastValue_StaysClampedAtLast()
        {
            // "Clamped" implies it should not wrap back to First.
            Assert.AreEqual(SampleEnum.Third, SampleEnum.Third.NextClamped());
        }
    }
}
