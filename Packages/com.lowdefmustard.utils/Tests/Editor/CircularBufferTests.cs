using NUnit.Framework;

namespace LowDefMustard.Utils.Tests.Editor
{
    public class CircularBufferTests
    {
        [Test]
        public void Add_BelowCapacity_IncreasesSize()
        {
            var buffer = new CircularBuffer<int>(size: 3);

            buffer.Add(1);
            buffer.Add(2);

            Assert.AreEqual(2, buffer.GetCurrentSize());
        }

        [Test]
        public void Add_NewestEntry_BecomesFirstEntry()
        {
            var buffer = new CircularBuffer<int>(size: 3);

            buffer.Add(1);
            buffer.Add(2);

            // AddFirst pushes the newest entry to the front.
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

            // Buffer is full; this should push out the oldest entry (1).
            buffer.Add(5);

            Assert.AreEqual(4, buffer.GetCurrentSize());
            Assert.AreEqual(5, buffer.GetFirstEntry());
            Assert.AreEqual(2, buffer.GetLastEntry());
        }

        [Test]
        public void GetEntryAtPosition_Zero_ReturnsFirstEntry()
        {
            var buffer = new CircularBuffer<int>(size: 3);
            buffer.Add(1);
            buffer.Add(2);

            Assert.AreEqual(buffer.GetFirstEntry(), buffer.GetEntryAtPosition(0));
        }

        [Test]
        public void GetEntryAtPosition_WellBeyondSize_ReturnsDefault()
        {
            var buffer = new CircularBuffer<int>(size: 3);
            buffer.Add(1);

            // Comfortably out of range - should hit the position > queue.Count guard.
            Assert.AreEqual(default(int), buffer.GetEntryAtPosition(10));
        }

        [Test]
        public void GetEntryAtPosition_ExactlyAtCount_ReturnsDefault()
        {
            var buffer = new CircularBuffer<int>(size: 3);
            buffer.Add(1);
            buffer.Add(2);

            Assert.AreEqual(default(int), buffer.GetEntryAtPosition(buffer.GetCurrentSize()));
        }

        [Test]
        public void Clear_ResetsSizeToZero()
        {
            var buffer = new CircularBuffer<int>(size: 3);
            buffer.Add(1);
            buffer.Add(2);

            buffer.Clear();

            Assert.AreEqual(0, buffer.GetCurrentSize());
        }

        [Test]
        public void GetFirstEntry_OnEmptyBuffer_ReturnsDefault()
        {
            var buffer = new CircularBuffer<int>(size: 3);
            
            Assert.AreEqual(default(int), buffer.GetFirstEntry());
        }
    }
}
