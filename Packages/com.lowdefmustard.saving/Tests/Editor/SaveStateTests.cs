using NUnit.Framework;

namespace LowDefMustard.Saving.Tests.Editor
{
    public class SaveStateTests
    {
        private class PositionData
        {
            public float x;
            public float y;
        }

        [Test]
        public void Constructor_SetsLoadPriority()
        {
            var saveState = new SaveState(LoadPriority.ObjectProperty, 42);
            Assert.AreEqual(LoadPriority.ObjectProperty, saveState.GetLoadPriority());
        }

        [Test]
        public void TryGetState_ScalarRoundTrip_ReturnsTrueAndValue()
        {
            var saveState = new SaveState(LoadPriority.ObjectInstantiation, 7);
            bool result = saveState.TryGetState(out int value);

            Assert.IsTrue(result);
            Assert.AreEqual(7, value);
        }

        [Test]
        public void TryGetState_ObjectRoundTrip_ReturnsTrueAndValue()
        {
            var original = new PositionData { x = 3f, y = 4f };
            var saveState = new SaveState(LoadPriority.ObjectProperty, original);
            bool result = saveState.TryGetState(out PositionData value);

            Assert.IsTrue(result);
            Assert.AreEqual(3f, value.x);
            Assert.AreEqual(4f, value.y);
        }

        [Test]
        public void TryGetState_MismatchedType_ReturnsFalse()
        {
            // State was captured as a string - asking for it back as an int should fail, not throw
            var saveState = new SaveState(LoadPriority.ObjectProperty, "not a number");
            bool result = saveState.TryGetState(out int value);

            Assert.IsFalse(result);
        }
    }
}
