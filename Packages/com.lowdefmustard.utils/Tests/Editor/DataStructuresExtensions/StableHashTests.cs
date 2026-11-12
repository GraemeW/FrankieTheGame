using NUnit.Framework;

namespace LowDefMustard.Utils.Tests.Editor
{
    public class StableHashTests
    {
        [Test]
        public void GetOffsetBasis_ReturnsFnvOffsetBasis()
        {
            Assert.AreEqual(2166136261u, StableHash.GetOffsetBasis());
        }

        [TestCase("", 1268118805u)]
        [TestCase(null, 3809873841u)]
        [TestCase("abc", 4124749718u)]
        [TestCase("é中", 4290101237u)]
        public void Add_KnownInputs_ProduceFixedValues(string value, uint expected)
        {
            Assert.AreEqual(expected, StableHash.Add(StableHash.GetOffsetBasis(), value));
        }

        [Test]
        public void Add_ShiftedBoundary_ProducesDifferentHash()
        {
            uint first = StableHash.Add(StableHash.Add(StableHash.GetOffsetBasis(), "ab"), "c");
            uint second = StableHash.Add(StableHash.Add(StableHash.GetOffsetBasis(), "a"), "bc");

            Assert.AreNotEqual(first, second);
        }

        [Test]
        public void Add_NullVersusEmpty_ProducesDifferentHash()
        {
            Assert.AreNotEqual(StableHash.Add(StableHash.GetOffsetBasis(), null), StableHash.Add(StableHash.GetOffsetBasis(), ""));
        }
    }
}
