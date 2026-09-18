using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LowDefMustard.Utils.Tests
{
    public class LazyValueLifecycleTests
    {
        // NOTE ON TECHNIQUE:
        // AddComponent() runs Awake() synchronously if the GameObject is already active
        // To configure fields *before* Awake fires (mirroring setting values in the Inspector before pressing Play), create the GameObject inactive, AddComponent, configure fields, then SetActive(true)

        [UnityTest]
        public IEnumerator CaseA_ValueSetInAwake_InitializerNeverRuns()
        {
            var go = new GameObject("CaseA");
            go.SetActive(false);
            var comp = go.AddComponent<LazyValueCaseAExample>();
            comp.savedValueToApply = 99;
            comp.applySavedValueInAwake = true;

            go.SetActive(true); // Awake() runs now

            Assert.AreEqual(99, comp.score.value);
            Assert.AreEqual(0, comp.initializerCallCount); // fallback initializer never ran

            yield return null;
            Object.Destroy(go);
        }

        [UnityTest]
        public IEnumerator CaseA_WhenSavedValueNotApplied_FallsBackToInitializerOnFirstAccess()
        {
            var go = new GameObject("CaseA_NoSave");
            go.SetActive(false);
            var comp = go.AddComponent<LazyValueCaseAExample>();
            comp.applySavedValueInAwake = false; // simulate "no save data available"

            go.SetActive(true); // Awake() runs now, without ever setting .value

            Assert.AreEqual(0, comp.initializerCallCount); // still untouched immediately after Awake

            int result = comp.score.value; // first real access - falls back to the initializer

            Assert.AreEqual(-1, result);
            Assert.AreEqual(1, comp.initializerCallCount);

            yield return null;
            Object.Destroy(go);
        }

        [UnityTest]
        public IEnumerator CaseB_ForceInitInStart_RunsAfterAwakeButBeforeFirstAccess()
        {
            var go = new GameObject("CaseB");
            var comp = go.AddComponent<LazyValueCaseBExample>(); // Awake() runs now
            comp.SetDefaultScoreValueForTest(7);

            // Immediately after AddComponent, Awake() has run but Start() has not - Start() is deferred to Unity's next update, not called synchronously here
            Assert.AreEqual(0, comp.initializerCallCount);

            yield return null; // let the engine process a frame so Start() executes

            Assert.AreEqual(1, comp.initializerCallCount);
            Assert.AreEqual(7, comp.score.value);
            Assert.AreEqual(1, comp.initializerCallCount); // confirms .value above didn't re-run it

            Object.Destroy(go);
        }

        [UnityTest]
        public IEnumerator ReInitLazyValue_AfterCachedUnityObjectIsDestroyed_ReInitializesOnForceInit()
        {
            // PlayMode Critical -- Edit Mode tests can't reach this test, as it depends on a real Destroy() going through Unity's actual object lifecycle
            // This test ensures checked cached-value validity with `cachedValue != null` AND `.Equals()`
            // .Equals() will correctly invoke UnityEngine.Object's override
            var originalTargetGo = new GameObject("OriginalTarget");
            originalTargetGo.AddComponent<DummyFindableTarget>();
            var host = new GameObject("Host").AddComponent<ReInitLazyValueCachedReferenceExample>();

            Transform firstAccess = host.cachedTarget.value; // finder locates originalTargetGo
            Assert.AreEqual(1, host.initializerCallCount);
            Assert.AreSame(originalTargetGo.transform, firstAccess);

            Object.Destroy(originalTargetGo);
            yield return null; // Destroy() is deferred to end of frame

            // Unity's overridden == confirms the object is "destroyed" (fake-null)
            Assert.IsTrue(originalTargetGo == null);

            // Simulates a respawn -- a new instance appears elsewhere in the scene
            var respawnedTargetGo = new GameObject("RespawnedTarget");
            respawnedTargetGo.AddComponent<DummyFindableTarget>();

            host.cachedTarget.ForceInit();

            Assert.AreEqual(2, host.initializerCallCount, "Destroyed cached reference should trigger re-initialization");
            Assert.AreSame(respawnedTargetGo.transform, host.cachedTarget.value, "Re-init should find the new instance, not the stale destroyed one");

            Object.Destroy(host.gameObject);
            Object.Destroy(respawnedTargetGo);
        }

        [UnityTest]
        public IEnumerator TryGetSafely_AllowReInitTrue_AfterCachedUnityObjectDestroyed_RespawnScenario_ReturnsTrueWithNewValue()
        {
            var originalTargetGo = new GameObject("OriginalTarget");
            originalTargetGo.AddComponent<DummyFindableTarget>();
            var host = new GameObject("Host").AddComponent<ReInitLazyValueCachedReferenceExample>();

            bool firstResult = host.cachedTarget.TryGetSafely(out Transform firstValue);
            Assert.IsTrue(firstResult);
            Assert.AreSame(originalTargetGo.transform, firstValue);

            Object.Destroy(originalTargetGo);
            yield return null; // Destroy() is deferred to end of frame

            var respawnedTargetGo = new GameObject("RespawnedTarget");
            respawnedTargetGo.AddComponent<DummyFindableTarget>();

            bool secondResult = host.cachedTarget.TryGetSafely(out Transform secondValue);

            Assert.IsTrue(secondResult);
            Assert.AreSame(respawnedTargetGo.transform, secondValue);
            Assert.AreEqual(2, host.initializerCallCount);

            Object.Destroy(host.gameObject);
            Object.Destroy(respawnedTargetGo);
        }

        [UnityTest]
        public IEnumerator TryGetSafely_AllowReInitFalse_AfterCachedUnityObjectDestroyed_ReturnsFalseWithoutReInitializing()
        {
            // This is the actual motivating case: allowReInit: false must never attempt a fresh lookup
            var targetGo = new GameObject("Target");
            targetGo.AddComponent<DummyFindableTarget>();
            var host = new GameObject("Host").AddComponent<ReInitLazyValueCachedReferenceExample>();

            _ = host.cachedTarget.TryGetSafely(out _);
            Assert.AreEqual(1, host.initializerCallCount);

            Object.Destroy(targetGo);
            yield return null; // Destroy() is deferred to end of frame

            bool result = host.cachedTarget.TryGetSafely(out Transform _, allowReInit: false);

            Assert.IsFalse(result);
            Assert.AreEqual(1, host.initializerCallCount, "allowReInit: false should never trigger a fresh initializer call");

            Object.Destroy(host.gameObject);
        }

        [UnityTest]
        public IEnumerator SubscribeInOnEnable_UnSubscribeInOnDisable_CycleTests()
        {
            var sourceGo = new GameObject("Source");
            var eventSource = sourceGo.AddComponent<DummyEventSource>();
            var host = new GameObject("Host").AddComponent<ReInitLazyValueSubscribeUnsubscribeExample>();
            yield return null; // let OnEnable run and subscribe

            Assert.AreEqual(1, host.initializerCallCount);

            eventSource.Fire();
            Assert.AreEqual(1, host.eventReceivedCount);

            // OnEnable/OnDisable triggered via SetActive() aren't reliably synchronous (yield after the call, and confirm OnDisable actually ran)
            host.gameObject.SetActive(false);
            yield return null;

            Assert.IsTrue(host.onDisableRan, "OnDisable should have run by now");
            Assert.AreEqual(1, host.initializerCallCount, "OnDisable's safe unsubscribe should not have triggered a re-init attempt");
            
            for (int i = 0; i < 5; i++)
            {
                host.gameObject.SetActive(true);
                yield return null;
                eventSource.Fire(); // Fire - will receive
                host.gameObject.SetActive(false);
                yield return null;
                eventSource.Fire(); // Fire - will not receive (if unsub successful)
            }
            Assert.AreEqual(1, host.initializerCallCount); // Should not lose reference during OnDisable/OnEnable cycles
            Assert.AreEqual(6, host.eventReceivedCount); // Initial call (1) + loop count (5) = 6
            
            Object.Destroy(sourceGo);
            Object.Destroy(host.gameObject);
        }
        
        [UnityTest]
        public IEnumerator SubscribeInOnEnable_TargetDestroyedBeforeOnDisable_UnsubscribesSafelyWithoutReinitializing()
        {
            var sourceGo = new GameObject("Source");
            var eventSource = sourceGo.AddComponent<DummyEventSource>();
            var host = new GameObject("Host").AddComponent<ReInitLazyValueSubscribeUnsubscribeExample>();
            yield return null; // let OnEnable run and subscribe

            Assert.AreEqual(1, host.initializerCallCount);

            eventSource.Fire();
            Assert.AreEqual(1, host.eventReceivedCount);

            Object.Destroy(sourceGo);
            yield return null; // Destroy deferred to end of frame
            Assert.IsTrue(eventSource == null); // confirms it's really gone (fake-null)

            // OnEnable/OnDisable triggered via SetActive() aren't reliably synchronous (yield after the call, and confirm OnDisable actually ran)
            host.gameObject.SetActive(false);
            yield return null;

            Assert.IsTrue(host.onDisableRan, "OnDisable should have run by now");
            Assert.AreEqual(1, host.initializerCallCount, "OnDisable's safe unsubscribe should not have triggered a re-init attempt");

            Object.Destroy(host.gameObject);
        }
    }
}
