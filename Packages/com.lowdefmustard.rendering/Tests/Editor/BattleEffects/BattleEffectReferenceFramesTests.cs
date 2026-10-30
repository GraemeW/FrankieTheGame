using System.IO;
using NUnit.Framework;
using UnityEngine;
using LowDefMustard.Rendering.Editor;

namespace LowDefMustard.Rendering.Tests.Editor
{
    public class BattleEffectReferenceFramesTests
    {
        // Note:  Writes real PNGs to a scratch folder under Temp/, cleaned up in Teardown

        // Const Tunables
        private const string _folder = "Temp/_TEMP_BattleEffectReferenceFrames_SafeToDelete";

        // State
        private BattleEffectReferenceFrames referenceFrames;

        #region Setup
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            if (Directory.Exists(_folder)) { Directory.Delete(_folder, true); }
            Directory.CreateDirectory(_folder);

            // Written out of order + sized by frame (2, 3, 4 px) to check the load order
            WriteFrame("frame_0003.png", 4);
            WriteFrame("frame_0001.png", 2);
            WriteFrame("frame_0002.png", 3);
            File.WriteAllText(Path.Combine(_folder, "notes.txt"), "not a frame");
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            if (Directory.Exists(_folder)) { Directory.Delete(_folder, true); }
        }

        [SetUp]
        public void SetUp()
        {
            referenceFrames = new BattleEffectReferenceFrames(_folder);
        }

        [TearDown]
        public void TearDown()
        {
            referenceFrames.Dispose();
        }
        
        private static void WriteFrame(string fileName, int size)
        {
            var texture = new Texture2D(size, size);
            File.WriteAllBytes(Path.Combine(_folder, fileName), texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
        }
        #endregion

        #region Tests
        [Test]
        public void Constructor_LoadsOnlyPngs()
        {
            Assert.AreEqual(3, referenceFrames.count);
        }

        [Test]
        public void TryGetSize_IsTheFirstFramesSize()
        {
            Assert.IsTrue(referenceFrames.TryGetSize(out Vector2 size));
            Assert.AreEqual(new Vector2(2f, 2f), size);
        }

        [TestCase(0f, 0)]
        [TestCase(0.99f, 0)]
        [TestCase(1f, 1)]
        [TestCase(2.5f, 2)]
        public void TryGetFrame_PicksFramesInNameOrder(float time, int expectedIndex)
        {
            Assert.IsTrue(referenceFrames.TryGetFrame(time, 1f, out Texture2D frame, out int frameIndex));
            Assert.AreEqual(expectedIndex, frameIndex);
            Assert.AreEqual(expectedIndex + 2, frame.width);
        }

        [TestCase(-1f, 0)]
        [TestCase(100f, 2)]
        public void TryGetFrame_OutOfRangeTime_IsClamped(float time, int expectedIndex)
        {
            Assert.IsTrue(referenceFrames.TryGetFrame(time, 1f, out _, out int frameIndex));
            Assert.AreEqual(expectedIndex, frameIndex);
        }

        [Test]
        public void TryGetFrame_NoFrameRate_ReturnsFalse()
        {
            Assert.IsFalse(referenceFrames.TryGetFrame(0f, 0f, out Texture2D frame, out int frameIndex));
            Assert.IsNull(frame);
            Assert.AreEqual(-1, frameIndex);
        }

        [Test]
        public void Constructor_MissingFolder_IsEmpty()
        {
            using var missing = new BattleEffectReferenceFrames(Path.Combine(_folder, "missing"));

            Assert.AreEqual(0, missing.count);
            Assert.IsFalse(missing.TryGetSize(out _));
            Assert.IsFalse(missing.TryGetFrame(0f, 30f, out _, out _));
        }

        [Test]
        public void Dispose_ClearsFrames()
        {
            referenceFrames.Dispose();

            Assert.AreEqual(0, referenceFrames.count);
        }
        #endregion
    }
}
