using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LowDefMustard.Control.Tests.Editor
{
    // Covered: ActiveInputReceiver and ReceiverModifiedData
    // Not Covered: IInputReceiver's MonoBehaviour implementations (lives outside this package)
    
    public class ReceiverStateTests
    {
        // State
        private readonly List<GameObject> spawnedGameObjects = new();

        #region DataStructures
        private class FakeInputReceiver : IInputReceiver
        {
            public GameObject gameObject { get; }
            public bool destroyQueued { get; set; }
            public bool? lastSetActiveInputValue;

            public FakeInputReceiver(GameObject go) { gameObject = go; }

            public Action<ControllerInputType> GetInputHandler() => _ => { };
            public void SubscribeToReceiverUpdates(bool enable, Action<ReceiverModifiedType, ReceiverModifiedData> action) { }
            public void SetActiveInput(bool active) { lastSetActiveInputValue = active; }
            public bool TrySetController(BaseController controller) => true;
        }
        #endregion
        
        #region Setup
        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in spawnedGameObjects.Where(go => go != null))
            {
                Object.DestroyImmediate(go);
            }

            spawnedGameObjects.Clear();
        }
        #endregion

        #region PrivateMethods
        private GameObject CreateGameObject(bool active)
        {
            var go = new GameObject("ReceiverStateTests_Target");
            go.SetActive(active);
            spawnedGameObjects.Add(go);
            return go;
        }
        #endregion

        [Test]
        public void Constructor_ActiveGameObject_CapturesEnabledTrue()
        {
            var fakeReceiver = new FakeInputReceiver(CreateGameObject(true));
            var activeInputReceiver = new ActiveInputReceiver(fakeReceiver, null);
            Assert.That(activeInputReceiver.isGameObjectEnabled, Is.True);
        }

        [Test]
        public void Constructor_InactiveGameObject_CapturesEnabledFalse()
        {
            var fakeReceiver = new FakeInputReceiver(CreateGameObject(false));
            var activeInputReceiver = new ActiveInputReceiver(fakeReceiver, null);
            Assert.That(activeInputReceiver.isGameObjectEnabled, Is.False);
        }

        [Test]
        public void Constructor_NullDisableCallbacks_DefaultsToNoOpInsteadOfNull()
        {
            var fakeReceiver = new FakeInputReceiver(CreateGameObject(true));
            var activeInputReceiver = new ActiveInputReceiver(fakeReceiver, null);
            Assert.That(activeInputReceiver.disableCallbacks, Is.Not.Null);
            Assert.DoesNotThrow(() => activeInputReceiver.disableCallbacks());
        }

        [Test]
        public void Constructor_ProvidedDisableCallbacks_PreservesReference()
        {
            var fakeReceiver = new FakeInputReceiver(CreateGameObject(true));
            bool wasCalled = false;
            var activeInputReceiver = new ActiveInputReceiver(fakeReceiver, () => wasCalled = true);
            activeInputReceiver.disableCallbacks();
            Assert.That(wasCalled, Is.True);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void EnableInput_ForwardsValueToReceiver(bool active)
        {
            var fakeReceiver = new FakeInputReceiver(CreateGameObject(true));
            var activeInputReceiver = new ActiveInputReceiver(fakeReceiver, null);
            activeInputReceiver.EnableInput(active);
            Assert.That(fakeReceiver.lastSetActiveInputValue, Is.EqualTo(active));
        }

        [Test]
        public void ReceiverModifiedData_DefaultWritingState_IsFalse()
        {
            var fakeReceiver = new FakeInputReceiver(CreateGameObject(true));
            var data = new ReceiverModifiedData(fakeReceiver);
            Assert.That(data.inputReceiver, Is.SameAs(fakeReceiver));
            Assert.That(data.writingState, Is.False);
        }

        [Test]
        public void ReceiverModifiedData_ExplicitWritingState_IsPreserved()
        {
            var fakeReceiver = new FakeInputReceiver(CreateGameObject(true));
            var data = new ReceiverModifiedData(fakeReceiver, true);
            Assert.That(data.writingState, Is.True);
        }
    }
}
