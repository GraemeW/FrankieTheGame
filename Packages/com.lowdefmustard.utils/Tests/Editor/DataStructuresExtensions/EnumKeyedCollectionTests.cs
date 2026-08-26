using System.Linq;
using NUnit.Framework;

namespace LowDefMustard.Utils.Tests.Editor
{
    public class EnumKeyedCollectionTests
    {
        private enum SampleEnum
        {
            Alpha,
            Beta,
            Gamma
        }

        [Test]
        public void TryGet_KeyNeverSet_ReturnsFalseAndDefault()
        {
            var collection = new EnumKeyedCollection<SampleEnum, int>();

            bool found = collection.TryGet(SampleEnum.Alpha, out int value);

            Assert.IsFalse(found);
            Assert.AreEqual(default(int), value);
        }

        [Test]
        public void Set_ThenTryGet_ReturnsStoredValue()
        {
            var collection = new EnumKeyedCollection<SampleEnum, int>();

            collection.Set(SampleEnum.Beta, 10);
            bool found = collection.TryGet(SampleEnum.Beta, out int value);

            Assert.IsTrue(found);
            Assert.AreEqual(10, value);
        }

        [Test]
        public void Set_ExistingKey_UpdatesRatherThanDuplicates()
        {
            var collection = new EnumKeyedCollection<SampleEnum, int>();

            collection.Set(SampleEnum.Beta, 10);
            collection.Set(SampleEnum.Beta, 20);

            // Every enum value yields exactly one entry from the enumerator, so a duplicate entry for Beta would show up as a repeated key here
            var betaEntries = collection.Where(pair => pair.key == SampleEnum.Beta).ToList();

            Assert.AreEqual(1, betaEntries.Count);
            Assert.AreEqual(20, betaEntries[0].value);
        }

        [Test]
        public void GetEnumerator_YieldsEveryEnumValue_EvenUnsetOnes()
        {
            var collection = new EnumKeyedCollection<SampleEnum, int>();
            collection.Set(SampleEnum.Gamma, 99);

            var keys = collection.Select(pair => pair.key).ToList();

            CollectionAssert.AreEquivalent(
                new[] { SampleEnum.Alpha, SampleEnum.Beta, SampleEnum.Gamma },
                keys);
        }

        [Test]
        public void IEnumKeyedCollection_GetEnumType_ReturnsGenericEnumType()
        {
            var collection = new EnumKeyedCollection<SampleEnum, int>();
            IEnumKeyedCollection asInterface = collection;

            Assert.AreEqual(typeof(SampleEnum), asInterface.GetEnumType());
        }

        [Test]
        public void IEnumKeyedCollection_GetListName_ReturnsBackingFieldName()
        {
            // This couples the test to the private field name "entries" (FIXED DEFINITION)
            // If that field is ever renamed, this test (and SyncEntriesToEnum's SerializedProperty lookups in the drawer) both need updating together
            var collection = new EnumKeyedCollection<SampleEnum, int>();
            IEnumKeyedCollection asInterface = collection;

            Assert.AreEqual("entries", asInterface.GetListName());
        }

        [Test]
        public void SyncEntriesToEnum_AfterSet_PreservesExistingValues()
        {
            var collection = new EnumKeyedCollection<SampleEnum, int>();
            collection.Set(SampleEnum.Alpha, 5);
            IEnumKeyedCollection asInterface = collection;

            asInterface.SyncEntriesToEnum();

            bool found = collection.TryGet(SampleEnum.Alpha, out int value);
            Assert.IsTrue(found);
            Assert.AreEqual(5, value);
        }

        // TESTING NOTE:
        // SyncEntriesToEnum's "drop orphaned entries for removed enum members" path isn't covered here
        // Reaching it needs an entry whose key isn't a member of the current enum, which isn't easy to construct from outside the class without reflection.
        // Potential consideration - testing e.g. via a second EnumKeyedCollection built against a wider enum, or a small reflection helper
    }
}
