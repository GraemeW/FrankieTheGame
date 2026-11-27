using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using LowDefMustard.Utils.Tests;

namespace LowDefMustard.Saving.Tests.Editor
{
    public class SavingSystemManualReadTests
    {
        // Tested:
        //  - TryManualGetData - reads back what SaveableEntity.TryCaptureState wrote (same state key)
        //  - TryManualGetData - false on:  no state, the entity absent, the component's state absent, no SaveableEntity on the component
        // Not Tested:
        //  - ManualGetFullState / file loading ~ reads the save folder on disk

        // State
        private readonly TestObjectTracker tracker = new();
        private GameObject testGameObject;
        private SaveableEntity entity;
        private TestTypedSaveableComponent saveable;

        #region Setup
        [SetUp]
        public void SetUp()
        {
            testGameObject = tracker.CreateGameObject("SavingSystemManualReadTestObject");
            entity = testGameObject.AddComponent<SaveableEntity>();
            saveable = testGameObject.AddComponent<TestTypedSaveableComponent>();
        }

        [TearDown]
        public void TearDown() => tracker.DestroyAll();
        #endregion

        #region Helpers
        private JObject CaptureFullState()
        {
            Assert.IsTrue(entity.TryCaptureState(null, out JToken entityState));
            return new JObject { [entity.GetUniqueIdentifier()] = entityState };
        }
        #endregion

        #region Tests
        [Test]
        public void TryManualGetData_CapturedState_ReadsTheComponentsData()
        {
            saveable.value = 42;
            JObject fullState = CaptureFullState();

            Assert.IsTrue(SavingSystem.TryManualGetData(fullState, saveable, out int data));
            Assert.AreEqual(42, data);
        }

        [Test]
        public void TryManualGetData_NullState_ReturnsFalse()
        {
            Assert.IsFalse(SavingSystem.TryManualGetData(null, saveable, out int data));
            Assert.AreEqual(0, data);
        }

        [Test]
        public void TryManualGetData_EntityNotInState_ReturnsFalse()
        {
            var fullState = new JObject { ["SomeOtherEntity"] = new JObject() };
            Assert.IsFalse(SavingSystem.TryManualGetData(fullState, saveable, out int _));
        }

        [Test]
        public void TryManualGetData_ComponentStateAbsent_ReturnsFalse()
        {
            var fullState = new JObject { [entity.GetUniqueIdentifier()] = new JObject() };
            Assert.IsFalse(SavingSystem.TryManualGetData(fullState, saveable, out int _));
        }

        [Test]
        public void TryManualGetData_NoSaveableEntity_ReturnsFalse()
        {
            JObject fullState = CaptureFullState();
            GameObject looseObject = tracker.CreateGameObject("LooseSaveable");
            var looseSaveable = looseObject.AddComponent<TestTypedSaveableComponent>();

            Assert.IsFalse(SavingSystem.TryManualGetData(fullState, looseSaveable, out int _));
        }
        #endregion
    }
}
