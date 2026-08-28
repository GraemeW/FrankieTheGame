using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LowDefMustard.Saving.Tests.Editor
{
    // Uses TestSaveableComponent as a scriptable ISaveableBase stand-in
    // Not Covered:  Editor-only Update() empty/duplicate-GUID detection
    //  - depends on the Editor's own Update cadence and SerializedObject/Selection state
    
    public class SaveableEntityTests
    {
        private GameObject testGameObject;
        private SaveableEntity entity;

        [SetUp]
        public void SetUp()
        {
            testGameObject = new GameObject("SaveableEntityTestObject");
            entity = testGameObject.AddComponent<SaveableEntity>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(testGameObject);
        }

        #region GetUniqueIdentifier
        [Test]
        public void GetUniqueIdentifier_Unset_GeneratesNonEmptyGuid()
        {
            string id = entity.GetUniqueIdentifier();

            Assert.IsFalse(string.IsNullOrWhiteSpace(id));
            Assert.IsTrue(System.Guid.TryParse(id, out _));
        }

        [Test]
        public void GetUniqueIdentifier_CalledTwice_IsStable()
        {
            string first = entity.GetUniqueIdentifier();
            string second = entity.GetUniqueIdentifier();

            Assert.AreEqual(first, second);
        }
        #endregion

        #region ForcePreventSave / IsSaveRestricted
        [Test]
        public void IsSaveRestricted_DefaultsFalse()
        {
            Assert.IsFalse(entity.IsSaveRestricted());
        }

        [Test]
        public void ForcePreventSave_SetsIsSaveRestrictedTrue()
        {
            entity.ForcePreventSave();
            Assert.IsTrue(entity.IsSaveRestricted());
        }
        #endregion

        #region TryCaptureState
        [Test]
        public void TryCaptureState_NoComponents_ReturnsFalse()
        {
            bool result = entity.TryCaptureState(null, out JToken _);
            Assert.IsFalse(result);
        }

        [Test]
        public void TryCaptureState_SaveRestricted_ReturnsFalse()
        {
            var component = testGameObject.AddComponent<TestSaveableComponent>();
            component.captureStateReturns = "value";
            entity.ForcePreventSave();

            bool result = entity.TryCaptureState(null, out JToken _);
            Assert.IsFalse(result);
        }

        [Test]
        public void TryCaptureState_ComponentReturnsState_AddsKeyedEntry()
        {
            var component = testGameObject.AddComponent<TestSaveableComponent>();
            component.captureStateReturns = "someValue";

            bool result = entity.TryCaptureState(null, out JToken updatedState);

            Assert.IsTrue(result);
            var stateObject = (JObject)updatedState;
            Assert.IsTrue(stateObject.ContainsKey(typeof(TestSaveableComponent).ToString()));
        }

        [Test]
        public void TryCaptureState_ComponentReturnsNullState_IsSkipped()
        {
            testGameObject.AddComponent<TestSaveableComponent>(); // captureStateReturns defaults to null

            bool result = entity.TryCaptureState(null, out JToken _);
            Assert.IsFalse(result);
        }

        [Test]
        public void TryCaptureState_OnlyCorePlayerState_SkipsNonCoreComponents()
        {
            var component = testGameObject.AddComponent<TestSaveableComponent>();
            component.captureStateReturns = "someValue";
            component.isCorePlayerState = false;

            bool result = entity.TryCaptureState(null, out JToken _, onlyCorePlayerState: true);
            Assert.IsFalse(result);
        }

        [Test]
        public void TryCaptureState_OnlyCorePlayerState_IncludesCoreComponents()
        {
            var component = testGameObject.AddComponent<TestSaveableComponent>();
            component.captureStateReturns = "someValue";
            component.isCorePlayerState = true;

            bool result = entity.TryCaptureState(null, out JToken _, onlyCorePlayerState: true);
            Assert.IsTrue(result);
        }
        #endregion

        #region RestoreState
        [Test]
        public void RestoreState_MalformedToken_LogsErrorAndDoesNotThrow()
        {
            testGameObject.AddComponent<TestSaveableComponent>();

            LogAssert.Expect(LogType.Warning, "Malformed data in save file");
            Assert.DoesNotThrow(() => entity.RestoreState(JValue.CreateNull(), LoadPriority.ObjectProperty));
        }

        [Test]
        public void RestoreState_MatchingTypeAndPriority_InvokesComponentRestoreState()
        {
            var component = testGameObject.AddComponent<TestSaveableComponent>();
            component.captureStateReturns = "capturedValue";
            component.loadPriority = LoadPriority.ObjectProperty;

            entity.TryCaptureState(null, out JToken capturedState);
            entity.RestoreState(capturedState, LoadPriority.ObjectProperty);

            Assert.IsTrue(component.restoreStateWasCalled);
            component.restoreStateReceivedValue.TryGetState(out string restoredValue);
            Assert.AreEqual("capturedValue", restoredValue);
        }

        [Test]
        public void RestoreState_MismatchedPriority_DoesNotInvokeComponentRestoreState()
        {
            var component = testGameObject.AddComponent<TestSaveableComponent>();
            component.captureStateReturns = "capturedValue";
            component.loadPriority = LoadPriority.ObjectInstantiation;

            entity.TryCaptureState(null, out JToken capturedState);
            entity.RestoreState(capturedState, LoadPriority.ObjectProperty);

            Assert.IsFalse(component.restoreStateWasCalled);
        }
        #endregion

        #region ApplyFinishingTouches
        [Test]
        public void ApplyFinishingTouches_InvokesAllComponents()
        {
            var component = testGameObject.AddComponent<TestSaveableComponent>();
            entity.ApplyFinishingTouches();

            Assert.IsTrue(component.applyFinishingTouchesWasCalled);
        }
        #endregion

        #region TryGetStateDictionary
        [Test]
        public void TryGetStateDictionary_NullReference_ReturnsFalse()
        {
            bool result = SaveableEntity.TryGetStateDictionary(null, out JObject dictionary);

            Assert.IsFalse(result);
            Assert.IsNull(dictionary);
        }

        [Test]
        public void TryGetStateDictionary_ValidObjectToken_ReturnsTrue()
        {
            JToken token = new JObject { ["key"] = "value" };
            bool result = SaveableEntity.TryGetStateDictionary(token, out JObject dictionary);

            Assert.IsTrue(result);
            Assert.AreEqual("value", dictionary["key"]?.ToObject<string>());
        }

        [Test]
        public void TryGetStateDictionary_JsonNullToken_ReturnsFalseAndLogsError()
        {
            LogAssert.Expect(LogType.Warning, "Malformed data in save file");
            bool result = SaveableEntity.TryGetStateDictionary(JValue.CreateNull(), out JObject dictionary);

            Assert.IsFalse(result);
        }
        #endregion

        #region ManualCaptureSaveState
        [Test]
        public void ManualCaptureSaveState_NullDictionary_CreatesNewAndAddsEntry()
        {
            var saveState = new SaveState(LoadPriority.ObjectProperty, "value");
            JObject result = SaveableEntity.ManualCaptureSaveState(null, "SomeType", saveState);

            Assert.IsTrue(result.ContainsKey("SomeType"));
        }

        [Test]
        public void ManualCaptureSaveState_ExistingDictionary_OverwritesKey()
        {
            var existing = new JObject { ["SomeType"] = "oldValue" };
            var saveState = new SaveState(LoadPriority.ObjectProperty, "newValue");

            JObject result = SaveableEntity.ManualCaptureSaveState(existing, "SomeType", saveState);

            JToken token = result["SomeType"];
            if (token == null)
            {
                Debug.LogError("Error:  No token found for `SomeType` - something went wrong.");
                return;
            }

            token.ToObject<SaveState>().TryGetState(out string value);
            Assert.AreEqual("newValue", value);
        }
        #endregion
    }
}
