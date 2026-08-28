using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System.Linq;

namespace LowDefMustard.Saving.Tests.Editor
{
    // Covered:  SavingSystem's pure JObject-manipulation helpers
    // Not Covered:  File I/O && SaveableEntity/GameObject dependency
    
    public class SavingSystemStateHelpersTests
    {
        [Test]
        public void ManualGetLastScene_NullState_ReturnsEmptyString()
        {
            Assert.AreEqual(string.Empty, SavingSystem.ManualGetLastScene(null));
        }

        [Test]
        public void ManualGetLastScene_NoSceneKeySet_ReturnsEmptyString()
        {
            var state = new JObject();
            Assert.AreEqual(string.Empty, SavingSystem.ManualGetLastScene(state));
        }

        [Test]
        public void ManualUpdateLastScene_ThenManualGetLastScene_RoundTrips()
        {
            var state = new JObject();
            SavingSystem.ManualUpdateLastScene(state, "TestScene");

            Assert.AreEqual("TestScene", SavingSystem.ManualGetLastScene(state));
        }

        [Test]
        public void ManualAddOverWriteToState_EmptyIdentifier_DoesNotAddEntry()
        {
            var state = new JObject();
            SavingSystem.ManualAddOverWriteToState(state, new JObject { ["x"] = 1 }, "");

            Assert.IsFalse(state.Properties().Any());
        }

        [Test]
        public void ManualAddOverWriteToState_NullTokenToAdd_StoresEmptyObject()
        {
            var state = new JObject();
            SavingSystem.ManualAddOverWriteToState(state, null, "someId");

            Assert.IsTrue(state.ContainsKey("someId"));
            if (state["someId"] is JObject stored) { Assert.IsFalse(stored.Properties().Any()); }
        }

        [Test]
        public void ManualAddOverWriteToState_ExistingIdentifier_Overwrites()
        {
            var state = new JObject { ["someId"] = new JObject { ["old"] = true } };
            SavingSystem.ManualAddOverWriteToState(state, new JObject { ["new"] = true }, "someId");

            if (state["someId"] is JObject stored)
            {
                Assert.IsFalse(stored.ContainsKey("old"));
                Assert.IsTrue(stored["new"]?.ToObject<bool>());
            }
        }
    }
}
