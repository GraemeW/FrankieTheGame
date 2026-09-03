using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LowDefMustard.Control.Tests.Editor
{
    // Covered: BaseController's receiver-stack lifecycle
    // - exercised via AddInputReceiver and the IInputReceiver callback delegate (captured from SubscribeToReceiverUpdates)
    // Not Covered: VerifyUnique - it calls Destroy(gameObject), which in Edit Mode logs an error and does not actually destroy the object - covered separately
    
    public class ControllerReceiverStackTests
    {
        // State
        private readonly List<GameObject> spawnedGameObjects = new();

        #region DataStructures
        // Fuller double vs. ReceiverStateTests' - tracks call history and captures the OnReceiverModified callback so tests can invoke it directly
        private class FakeInputReceiver : IInputReceiver
        {
            public GameObject gameObject { get; }
            public bool destroyQueued { get; set; }
            public bool trySetControllerResult = true;
            public BaseController controllerPassedToTrySetController;
            public readonly List<bool> setActiveInputCalls = new();
            public int getInputHandlerCallCount;
            public Action<ReceiverModifiedType, ReceiverModifiedData> receiverModifiedCallback;

            public FakeInputReceiver(GameObject go) { gameObject = go; }

            public Action<ControllerInputType> GetInputHandler()
            {
                getInputHandlerCallCount++;
                return _ => { };
            }

            public void SubscribeToReceiverUpdates(bool enable, Action<ReceiverModifiedType, ReceiverModifiedData> action) => receiverModifiedCallback = enable ? action : null;

            public void SetActiveInput(bool active) => setActiveInputCalls.Add(active);

            public bool TrySetController(BaseController controller)
            {
                controllerPassedToTrySetController = controller;
                return trySetControllerResult;
            }
        }

        private class TestController : BaseController
        {
            public bool onNoReceiversIdentifiedCalled;
            public bool hasAlternateReceiversActiveValue;
            public bool shouldDestroyForNoReceiversValue;

            // Widen access protected -> public in subclass
            public bool destroyQueuedValue => destroyQueued;

            protected override void OnNoReceiversIdentified() => onNoReceiversIdentifiedCalled = true;
            protected override bool HasAlternateReceiversActive() => hasAlternateReceiversActiveValue;
            protected override bool ShouldDestroyForNoReceivers() => shouldDestroyForNoReceiversValue;
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
        private GameObject CreateGameObject(bool active = true)
        {
            var go = new GameObject("ControllerReceiverStackTests_Target");
            go.SetActive(active);
            spawnedGameObjects.Add(go);
            return go;
        }

        private TestController CreateController()
        {
            GameObject go = CreateGameObject();
            var controller = go.AddComponent<TestController>();
            // Force a known polling interval rather than depending on the production default staying 0.1f
            var serializedController = new SerializedObject(controller);
            serializedController.FindProperty("listenerPollingInterval").floatValue = 0.1f;
            serializedController.ApplyModifiedPropertiesWithoutUndo();
            return controller;
        }
        #endregion

        #region AddInputReceiverTests
        [Test]
        public void AddInputReceiver_NullReceiver_DoesNotThrow()
        {
            TestController controller = CreateController();
            Assert.DoesNotThrow(() => controller.AddInputReceiver(null, null));
        }

        [Test]
        public void AddInputReceiver_TrySetControllerFails_SkipsSubscriptionAndEnable()
        {
            TestController controller = CreateController();
            var fake = new FakeInputReceiver(CreateGameObject()) { trySetControllerResult = false };

            controller.AddInputReceiver(fake, null);

            Assert.That(fake.setActiveInputCalls, Is.Empty);
            Assert.That(fake.receiverModifiedCallback, Is.Null);
        }

        [Test]
        public void AddInputReceiver_SingleReceiver_PassesControllerSubscribesAndEnables()
        {
            TestController controller = CreateController();
            var fake = new FakeInputReceiver(CreateGameObject());

            controller.AddInputReceiver(fake, null);

            Assert.That(fake.controllerPassedToTrySetController, Is.SameAs(controller));
            Assert.That(fake.receiverModifiedCallback, Is.Not.Null);
            Assert.That(fake.setActiveInputCalls, Is.EqualTo(new[] { true }));
            Assert.That(fake.getInputHandlerCallCount, Is.GreaterThan(0));
        }

        [Test]
        public void AddInputReceiver_SecondReceiver_DisablesFirstAndEnablesSecond()
        {
            TestController controller = CreateController();
            var fake1 = new FakeInputReceiver(CreateGameObject());
            var fake2 = new FakeInputReceiver(CreateGameObject());

            controller.AddInputReceiver(fake1, null);
            controller.AddInputReceiver(fake2, null);

            Assert.That(fake1.setActiveInputCalls, Is.EqualTo(new[] { true, false }));
            Assert.That(fake2.setActiveInputCalls, Is.EqualTo(new[] { true }));
        }
        #endregion

        #region ClientDisableClientEnablePushPopTests
        [Test]
        public void ClientDisable_ReEnablesPreviousReceiverAndInvokesDisableCallback()
        {
            TestController controller = CreateController();
            var fake1 = new FakeInputReceiver(CreateGameObject());
            var fake2 = new FakeInputReceiver(CreateGameObject());
            bool fake2DisableCallbackInvoked = false;

            controller.AddInputReceiver(fake1, null);
            controller.AddInputReceiver(fake2, () => fake2DisableCallbackInvoked = true);
            fake2.receiverModifiedCallback(ReceiverModifiedType.ClientDisable, new ReceiverModifiedData(fake2));

            Assert.That(fake2DisableCallbackInvoked, Is.True);
            Assert.That(fake1.setActiveInputCalls, Is.EqualTo(new[] { true, false, true }));
            // HandleReceiverDisable never calls EnableInput(false) on the receiver disabling itself
            //  - the receiver's own client is expected to already have turned off its own input
            //  - or, in a real use case, ClientDisable would be called during OnDisable
            Assert.That(fake2.setActiveInputCalls, Is.EqualTo(new[] { true }));
        }

        [Test]
        public void ClientDisableThenClientEnable_RestoresOriginalActiveReceiver()
        {
            TestController controller = CreateController();
            var fake1 = new FakeInputReceiver(CreateGameObject());
            var fake2 = new FakeInputReceiver(CreateGameObject());

            controller.AddInputReceiver(fake1, null);
            controller.AddInputReceiver(fake2, null);
            fake2.receiverModifiedCallback(ReceiverModifiedType.ClientDisable, new ReceiverModifiedData(fake2));
            fake2.receiverModifiedCallback(ReceiverModifiedType.ClientEnable, new ReceiverModifiedData(fake2));

            Assert.That(fake1.setActiveInputCalls, Is.EqualTo(new[] { true, false, true, false }));
            Assert.That(fake2.setActiveInputCalls, Is.EqualTo(new[] { true, true }));
        }

        [Test]
        public void ClearDisableCallbacks_PreventsLaterDisableCallbackFromFiring()
        {
            TestController controller = CreateController();
            var fake1 = new FakeInputReceiver(CreateGameObject());
            var fake2 = new FakeInputReceiver(CreateGameObject());
            bool fake2DisableCallbackInvoked = false;

            controller.AddInputReceiver(fake1, null);
            controller.AddInputReceiver(fake2, () => fake2DisableCallbackInvoked = true);
            fake2.receiverModifiedCallback(ReceiverModifiedType.ClearDisableCallbacks, new ReceiverModifiedData(fake2));
            fake2.receiverModifiedCallback(ReceiverModifiedType.ClientDisable, new ReceiverModifiedData(fake2));

            Assert.That(fake2DisableCallbackInvoked, Is.False);
        }

        [Test]
        public void OnReceiverModified_NullData_IsNoOp()
        {
            TestController controller = CreateController();
            var fake1 = new FakeInputReceiver(CreateGameObject());
            controller.AddInputReceiver(fake1, null);

            Assert.DoesNotThrow(() => fake1.receiverModifiedCallback(ReceiverModifiedType.ClientDisable, null));
            Assert.That(fake1.setActiveInputCalls, Is.EqualTo(new[] { true }));
        }
        #endregion

        #region ClientExitOnNoActiveReceiversTests
        [Test]
        public void ClientExit_UnsubscribesFromReceiverUpdates()
        {
            TestController controller = CreateController();
            var fake1 = new FakeInputReceiver(CreateGameObject());
            controller.AddInputReceiver(fake1, null);

            fake1.receiverModifiedCallback(ReceiverModifiedType.ClientExit, new ReceiverModifiedData(fake1));

            Assert.That(fake1.receiverModifiedCallback, Is.Null);
        }

        [Test]
        public void ClientExit_LastReceiver_ShouldDestroyFalse_DoesNotQueueDestroy()
        {
            TestController controller = CreateController();
            var fake1 = new FakeInputReceiver(CreateGameObject());
            controller.AddInputReceiver(fake1, null);

            fake1.receiverModifiedCallback(ReceiverModifiedType.ClientExit, new ReceiverModifiedData(fake1));

            Assert.That(controller.destroyQueuedValue, Is.False);
        }

        [Test]
        public void ClientExit_LastReceiver_ShouldDestroyTrue_QueuesDestroy()
        {
            TestController controller = CreateController();
            controller.shouldDestroyForNoReceiversValue = true;
            var fake1 = new FakeInputReceiver(CreateGameObject());
            controller.AddInputReceiver(fake1, null);

            fake1.receiverModifiedCallback(ReceiverModifiedType.ClientExit, new ReceiverModifiedData(fake1));

            Assert.That(controller.destroyQueuedValue, Is.True);
        }

        [Test]
        public void OnNoActiveReceivers_NoAlternatesActive_ClearsStashedDisabledReceivers()
        {
            TestController controller = CreateController();
            controller.hasAlternateReceiversActiveValue = false;
            var fake1 = new FakeInputReceiver(CreateGameObject());
            var fake2 = new FakeInputReceiver(CreateGameObject());

            controller.AddInputReceiver(fake1, null);
            controller.AddInputReceiver(fake2, null);
            // fake1 is now stashed-disabled (not destroyed) in the stack; destroying fake2 leaves nobody with isGameObjectEnabled == true, triggering OnNoActiveReceivers
            fake1.receiverModifiedCallback(ReceiverModifiedType.ClientDisable, new ReceiverModifiedData(fake1));
            fake2.receiverModifiedCallback(ReceiverModifiedType.ClientExit, new ReceiverModifiedData(fake2));

            // With no alternates active, the stack gets cleared - fake1's stale entry is gone, so a later ClientEnable for it is silently ignored (never found)
            fake1.receiverModifiedCallback(ReceiverModifiedType.ClientEnable, new ReceiverModifiedData(fake1));

            Assert.That(fake1.setActiveInputCalls, Is.EqualTo(new[] { true, false }));
        }

        [Test]
        public void OnNoActiveReceivers_AlternatesActive_PreservesStashedDisabledReceivers()
        {
            TestController controller = CreateController();
            var fake1 = new FakeInputReceiver(CreateGameObject());
            var fake2 = new FakeInputReceiver(CreateGameObject());

            controller.AddInputReceiver(fake1, null);
            controller.AddInputReceiver(fake2, null);
            fake1.receiverModifiedCallback(ReceiverModifiedType.ClientDisable, new ReceiverModifiedData(fake1));
            // Alternates are active from this point on, so OnNoActiveReceivers should not clear the stack
            controller.hasAlternateReceiversActiveValue = true;
            fake2.receiverModifiedCallback(ReceiverModifiedType.ClientExit, new ReceiverModifiedData(fake2));

            // fake1's entry should have survived, so it can be found and re-enabled
            fake1.receiverModifiedCallback(ReceiverModifiedType.ClientEnable, new ReceiverModifiedData(fake1));

            Assert.That(fake1.setActiveInputCalls, Is.EqualTo(new[] { true, false, true }));
        }
        #endregion

        #region PollForReceiversTests
        [Test]
        public void PollForReceivers_BelowThreshold_DoesNotTrigger()
        {
            TestController controller = CreateController();
            controller.PollForReceivers(0.05f);
            Assert.That(controller.onNoReceiversIdentifiedCalled, Is.False);
            Assert.That(controller.destroyQueuedValue, Is.False);
        }

        [Test]
        public void PollForReceivers_AccumulatesAcrossCallsPastThreshold_TriggersOnce()
        {
            TestController controller = CreateController();
            controller.PollForReceivers(0.05f);
            controller.PollForReceivers(0.06f);
            Assert.That(controller.onNoReceiversIdentifiedCalled, Is.True);
            Assert.That(controller.destroyQueuedValue, Is.True);
        }

        [Test]
        public void PollForReceivers_HasActiveReceiver_DoesNotTrigger()
        {
            TestController controller = CreateController();
            var fake1 = new FakeInputReceiver(CreateGameObject());
            controller.AddInputReceiver(fake1, null);

            controller.PollForReceivers(0.2f);

            Assert.That(controller.onNoReceiversIdentifiedCalled, Is.False);
        }

        [Test]
        public void PollForReceivers_HasAlternateReceiversActive_DoesNotTrigger()
        {
            TestController controller = CreateController();
            controller.hasAlternateReceiversActiveValue = true;

            controller.PollForReceivers(0.2f);

            Assert.That(controller.onNoReceiversIdentifiedCalled, Is.False);
        }
        #endregion
    }
}
