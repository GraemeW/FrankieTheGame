using System.Collections.Generic;
using NUnit.Framework;

namespace LowDefMustard.Utils.Tests.Editor
{
    public class ListExtensionsTests
    {
        [Test]
        public void Shuffle_PreservesAllOriginalElements()
        {
            var list = new List<int> { 1, 2, 3, 4, 5 };

            list.Shuffle();

            CollectionAssert.AreEquivalent(new[] { 1, 2, 3, 4, 5 }, list);
        }

        [Test]
        public void Shuffle_PreservesCount()
        {
            var list = new List<int> { 1, 2, 3, 4, 5 };

            list.Shuffle();

            Assert.AreEqual(5, list.Count);
        }

        [Test]
        public void Shuffle_EmptyList_DoesNotThrow()
        {
            var list = new List<int>();

            Assert.DoesNotThrow(() => list.Shuffle());
        }

        [Test]
        public void Shuffle_SingleElementList_UnchangedAndDoesNotThrow()
        {
            var list = new List<int> { 42 };

            Assert.DoesNotThrow(() => list.Shuffle());
            Assert.AreEqual(42, list[0]);
        }

        [Test]
        public void Shuffle_ManyTrials_ProducesMoreThanOneOrdering()
        {
            // Statistical/sanity check, not a strict guarantee - confirms Shuffle isn't a no-op or always producing the same permutation
            // 6 elements gives 720 possible orderings, so only one result across 30 trials would be exceptionally unlikely
            var distinctOrderings = new HashSet<string>();
            for (int i = 0; i < 30; i++)
            {
                var list = new List<int> { 1, 2, 3, 4, 5, 6 };
                list.Shuffle();
                distinctOrderings.Add(string.Join(",", list));
            }

            Assert.Greater(distinctOrderings.Count, 1);
        }
    }
}
