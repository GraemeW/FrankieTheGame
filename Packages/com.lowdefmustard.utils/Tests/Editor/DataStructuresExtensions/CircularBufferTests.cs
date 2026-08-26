using System.Linq;
using NUnit.Framework;

namespace LowDefMustard.Utils.Tests.Editor
{
    public class CircularBufferTests
    {
        [Test]
        public void Add_BelowCapacity_IncreasesCount()
        {
            var buffer = new CircularBuffer<int>(size: 4);
            buffer.Add(1);
            buffer.Add(2);

            Assert.AreEqual(2, buffer.GetCurrentSize());
        }

        [Test]
        public void Add_AtCapacity_CountStaysAtCapacity()
        {
            var buffer = new CircularBuffer<int>(size: 4);
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);
            buffer.Add(4);

            buffer.Add(5); // triggers eviction, not growth

            Assert.AreEqual(4, buffer.GetCurrentSize());
        }

        [Test]
        public void Add_NewestEntry_BecomesFirstEntry()
        {
            var buffer = new CircularBuffer<int>(size: 4);
            buffer.Add(1);
            buffer.Add(2);

            Assert.AreEqual(2, buffer.GetFirstEntry());
        }

        [Test]
        public void Add_AtCapacity_EvictsOldestEntry()
        {
            var buffer = new CircularBuffer<int>(size: 4);
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);
            buffer.Add(4);

            buffer.Add(5); // buffer full at [1,2,3,4]; this should evict the oldest (1)

            Assert.AreEqual(5, buffer.GetFirstEntry());
            Assert.AreEqual(2, buffer.GetLastEntry()); // 2 is now the oldest surviving entry
        }

        [Test]
        public void Add_MultipleWrapsAroundCapacity_KeepsOnlyMostRecentEntries()
        {
            var buffer = new CircularBuffer<int>(size: 4);
            for (int i = 1; i <= 10; i++)
            {
                buffer.Add(i);
            }

            // Only the last 4 adds (7,8,9,10) should have survived.
            CollectionAssert.AreEqual(new[] { 10, 9, 8, 7 }, buffer.ToList());
        }

        [Test]
        public void GetFirstEntry_OnEmptyBuffer_ReturnsDefault()
        {
            var buffer = new CircularBuffer<int>(size: 4);

            Assert.AreEqual(default(int), buffer.GetFirstEntry());
        }

        [Test]
        public void GetLastEntry_OnEmptyBuffer_ReturnsDefault()
        {
            var buffer = new CircularBuffer<int>(size: 4);

            Assert.AreEqual(default(int), buffer.GetLastEntry());
        }

        [Test]
        public void GetEntryAtPosition_Zero_MatchesGetFirstEntry()
        {
            var buffer = new CircularBuffer<int>(size: 4);
            buffer.Add(1);
            buffer.Add(2);

            Assert.AreEqual(buffer.GetFirstEntry(), buffer.GetEntryAtPosition(0));
        }

        [Test]
        public void GetEntryAtPosition_LastValidIndex_MatchesGetLastEntry()
        {
            var buffer = new CircularBuffer<int>(size: 4);
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);

            Assert.AreEqual(buffer.GetLastEntry(), buffer.GetEntryAtPosition(buffer.GetCurrentSize() - 1));
        }

        [Test]
        public void GetEntryAtPosition_ExactlyAtCount_ReturnsDefault()
        {
            var buffer = new CircularBuffer<int>(size: 4);
            buffer.Add(1);
            buffer.Add(2);

            Assert.AreEqual(default(int), buffer.GetEntryAtPosition(buffer.GetCurrentSize()));
        }

        [Test]
        public void GetEntryAtPosition_WellBeyondCount_ReturnsDefault()
        {
            var buffer = new CircularBuffer<int>(size: 4);
            buffer.Add(1);

            Assert.AreEqual(default(int), buffer.GetEntryAtPosition(50));
        }

        [Test]
        public void Clear_ResetsCountToZero()
        {
            var buffer = new CircularBuffer<int>(size: 4);
            buffer.Add(1);
            buffer.Add(2);

            buffer.Clear();

            Assert.AreEqual(0, buffer.GetCurrentSize());
        }

        [Test]
        public void Clear_AfterWrapAndClear_BehavesLikeFreshBuffer()
        {
            var buffer = new CircularBuffer<int>(size: 4);
            for (int i = 1; i <= 6; i++) buffer.Add(i); // force a wrap first

            buffer.Clear();
            buffer.Add(100);

            Assert.AreEqual(1, buffer.GetCurrentSize());
            Assert.AreEqual(100, buffer.GetFirstEntry());
        }

        [Test]
        public void GetEnumerator_YieldsEntriesNewestToOldest()
        {
            var buffer = new CircularBuffer<int>(size: 4);
            for (int i = 1; i <= 6; i++) buffer.Add(i); // wraps; logical contents are 3,4,5,6

            CollectionAssert.AreEqual(new[] { 6, 5, 4, 3 }, buffer.ToList());
        }

        [Test]
        public void Constructor_SizeAlreadyPowerOfTwo_UsesExactSizeAsCapacity()
        {
            var buffer = new CircularBuffer<int>(size: 4);
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);
            buffer.Add(4);

            // No eviction yet - capacity should be exactly 4, not rounded further.
            Assert.AreEqual(4, buffer.GetCurrentSize());
            Assert.AreEqual(1, buffer.GetLastEntry());
        }

        [Test]
        public void Constructor_NonPowerOfTwoSize_RoundsUpToNextPowerOfTwo()
        {
            var buffer = new CircularBuffer<int>(size: 5); // should round up to 8

            for (int i = 1; i <= 8; i++) buffer.Add(i);
            // If capacity were left at 5, entry 1 would already be evicted by now.
            Assert.AreEqual(1, buffer.GetLastEntry());

            buffer.Add(9); // the 9th add should now finally trigger eviction
            Assert.AreEqual(2, buffer.GetLastEntry());
        }

        [Test]
        public void Constructor_ZeroOrNegativeSize_DefaultsToCapacityTwo()
        {
            var buffer = new CircularBuffer<int>(size: 0);

            buffer.Add(1);
            buffer.Add(2);
            Assert.AreEqual(1, buffer.GetLastEntry()); // no eviction yet at capacity 2

            buffer.Add(3);
            Assert.AreEqual(2, buffer.GetLastEntry()); // now evicted
        }

        [Test]
        public void AsSpan_BeforeAnyWrap_MatchesInsertionOrder()
        {
            var buffer = new CircularBuffer<int>(size: 4);
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);

            var span = buffer.AsSpan();

            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, span.ToArray());
        }

        [Test]
        public void AsSpan_AfterWrap_ReflectsRawArrayOrderNotLogicalOrder()
        {
            // AsSpan() exposes the physical backing array for fast bulk mutation, but once the buffer has wrapped, physical slot order no longer matches newest-to-oldest logical order
            // Anyone using AsSpan() to iterate "in time order" after a wrap will get the wrong order
            var buffer = new CircularBuffer<int>(size: 4);
            for (int i = 1; i <= 5; i++) buffer.Add(i); // wraps once; physical buffer is [5,2,3,4]

            var span = buffer.AsSpan();

            CollectionAssert.AreEqual(new[] { 5, 2, 3, 4 }, span.ToArray());
            CollectionAssert.AreNotEqual(buffer.ToList(), span.ToArray());
        }
    }
}
