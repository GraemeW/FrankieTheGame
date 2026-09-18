using NUnit.Framework;

namespace LowDefMustard.Zones.Tests.Editor
{
    public class SceneQueueDataTests
    {
        [Test]
        public void FullConstructor_AssignsAllFields()
        {
            bool called = false;

            var data = new SceneQueueData(Callback, 1.5f, true);

            Assert.AreEqual(1.5f, data.delayTime);
            Assert.IsTrue(data.useFader);
            Assert.IsNotNull(data.sceneLoadedCallback);

            data.sceneLoadedCallback.Invoke();
            Assert.IsTrue(called);
            return;

            
            // Local Functions
            void Callback() => called = true;
        }

        [Test]
        public void UseFaderOnlyConstructor_DefaultsCallbackAndDelay()
        {
            var data = new SceneQueueData(false);

            Assert.IsFalse(data.useFader);
            Assert.AreEqual(0f, data.delayTime);
            Assert.IsNull(data.sceneLoadedCallback);
        }
    }
}
