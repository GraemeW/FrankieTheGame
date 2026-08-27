using System.Linq;
using NUnit.Framework;

namespace LowDefMustard.Utils.Tests.Editor
{
    public class EnumLookupTests
    {
        private enum SampleEnum
        {
            Alpha,
            Beta,
            Gamma
        }

        [Test]
        public void TryGet_ReferenceTypeValue_KeyNeverSet_ReturnsFalse()
        {
            var lookup = new EnumLookup<SampleEnum, string>();

            bool found = lookup.TryGet(SampleEnum.Alpha, out string value);

            Assert.IsFalse(found);
            Assert.IsNull(value);
        }

        [Test]
        public void TrySet_ThenTryGet_ReferenceTypeValue_ReturnsStoredValue()
        {
            var lookup = new EnumLookup<SampleEnum, string>();

            lookup.TrySet(SampleEnum.Beta, "hello");
            bool found = lookup.TryGet(SampleEnum.Beta, out string value);

            Assert.IsTrue(found);
            Assert.AreEqual("hello", value);
        }

        [Test]
        public void TryGet_WrongEnumType_ReturnsFalse()
        {
            var lookup = new EnumLookup<SampleEnum, string>();

            // Passing a System.Enum of a different concrete type than TEnum
            bool found = lookup.TryGet(System.DayOfWeek.Monday, out string value);

            Assert.IsFalse(found);
            Assert.IsNull(value);
        }

        [Test]
        public void GetValues_ReferenceTypeValue_SkipsUnsetKeys()
        {
            var lookup = new EnumLookup<SampleEnum, string>();
            lookup.TrySet(SampleEnum.Alpha, "a");
            lookup.TrySet(SampleEnum.Gamma, "c");
            // Beta deliberately left unset

            var values = lookup.GetValues<SampleEnum>().ToList();

            CollectionAssert.AreEquivalent(new[] { "a", "c" }, values);
        }

        [Test]
        public void TryGet_ValueTypeValue_KeyNeverSet_ReturnsFalse()
        {
            var lookup = new EnumLookup<SampleEnum, int>();

            bool found = lookup.TryGet(SampleEnum.Alpha, out int value);

            Assert.IsFalse(found);
            Assert.AreEqual(0, value); // default(int)
        }

        [Test]
        public void TrySet_ThenTryGet_ValueTypeValue_AlsoReturnsTrue()
        {
            var lookup = new EnumLookup<SampleEnum, int>();

            lookup.TrySet(SampleEnum.Alpha, 7);
            bool found = lookup.TryGet(SampleEnum.Alpha, out int value);

            Assert.IsTrue(found);
            Assert.AreEqual(7, value);
        }
    }
}
