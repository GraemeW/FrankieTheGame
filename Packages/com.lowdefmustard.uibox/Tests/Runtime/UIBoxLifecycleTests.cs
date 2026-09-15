using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LowDefMustard.UIBox.Tests
{
    // Test Notes:
    //  - Every test creates its TestUIBox inactive, then configures it before activating
    //      - this ordering guarantees configuration lands before Awake actually runs
    
    public class UIBoxLifecycleTests
    {
        // State
        private readonly List<GameObject> spawned = new();

        // Setup
        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in spawned)
            {
                if (go != null) { Object.Destroy(go); }
            }
            spawned.Clear();
        }

        #region PrivateMethods
        private TestUIBox CreateInactiveTestUIBox()
        {
            var go = new GameObject("TestUIBox", typeof(RectTransform));
            spawned.Add(go);
            go.SetActive(false);
            return go.AddComponent<TestUIBox>();
        }
        #endregion

        #region Tests
        [UnityTest]
        public IEnumerator Awake_TryAcquireDependenciesFails_DestroysGameObject()
        {
            TestUIBox testUIBox = CreateInactiveTestUIBox();
            testUIBox.tryAcquireDependenciesResult = false;

            testUIBox.gameObject.SetActive(true);
            yield return null;

            Assert.IsTrue(testUIBox == null);
        }

        [UnityTest]
        public IEnumerator Awake_DependenciesAcquired_TriggersAwakeAndSetsUpBackExitButton()
        {
            TestUIBox testUIBox = CreateInactiveTestUIBox();
            var optionParentGo = new GameObject("OptionParent", typeof(RectTransform));
            spawned.Add(optionParentGo);
            var backExitSourceGo = new GameObject("BackExitSource", typeof(RectTransform));
            spawned.Add(backExitSourceGo);
            UIBackExit backExitPrefab = backExitSourceGo.AddComponent<UIBackExit>();
            testUIBox.SetHookups(optionParentRef: optionParentGo.transform, backExitPrefabRef: backExitPrefab);

            testUIBox.gameObject.SetActive(true);
            yield return null;

            Assert.AreEqual(1, testUIBox.awakeTriggeredCount);
            Assert.IsNotNull(optionParentGo.GetComponentInChildren<UIBackExit>());
        }

        [UnityTest]
        public IEnumerator OnEnable_DiscoversChoiceOptionsUnderOptionParent()
        {
            TestUIBox testUIBox = CreateInactiveTestUIBox();
            var optionParentGo = new GameObject("OptionParent", typeof(RectTransform));
            spawned.Add(optionParentGo);
            UIChoiceButton childChoice = TestChoiceFactory.CreateWiredButton("ChildChoice", spawned);
            childChoice.transform.SetParent(optionParentGo.transform);
            testUIBox.SetHookups(optionParentRef: optionParentGo.transform);

            testUIBox.gameObject.SetActive(true);
            yield return null;

            Assert.AreEqual(1, testUIBox.enableTriggeredCount);
            CollectionAssert.Contains(testUIBox.publicChoiceOptions, childChoice);
            Assert.IsTrue(testUIBox.PublicIsChoiceAvailable());
        }

        [UnityTest]
        public IEnumerator OnDisable_ClearsHighlightAndTriggersDisable()
        {
            TestUIBox testUIBox = CreateInactiveTestUIBox();
            UIChoiceButton choice = TestChoiceFactory.CreateWiredButton("Choice", spawned);
            testUIBox.gameObject.SetActive(true);
            yield return null;
            testUIBox.InjectChoiceOptions(new UIChoice[] { choice });
            testUIBox.SetHighlightedChoiceOption(choice);

            testUIBox.gameObject.SetActive(false);

            Assert.AreEqual(1, testUIBox.disableTriggeredCount);
            Assert.IsNull(testUIBox.publicHighlightedChoiceOption);
        }

        [UnityTest]
        public IEnumerator OnDestroy_TriggersDestroyCallback()
        {
            TestUIBox testUIBox = CreateInactiveTestUIBox();
            testUIBox.gameObject.SetActive(true);
            yield return null;

            Object.Destroy(testUIBox.gameObject);
            yield return null;

            Assert.AreEqual(1, testUIBox.destroyTriggeredCount);
        }

        [UnityTest]
        public IEnumerator MissingControllerAfterStart_QueuesDestroyAfterOneFrameGrace()
        {
            TestUIBox testUIBox = CreateInactiveTestUIBox();
            testUIBox.gameObject.SetActive(true); // handleGlobalInput defaults true, no controller set

            // Grace period: Start() must run, then the coroutine's own yield return null must elapse
            yield return null;
            yield return null;

            Assert.IsTrue(testUIBox.destroyQueued);
        }

        [UnityTest]
        public IEnumerator ControllerSetBeforeGraceFrameElapses_IsNotDestroyed()
        {
            TestUIBox testUIBox = CreateInactiveTestUIBox();
            testUIBox.gameObject.SetActive(true);
            yield return null; // let Start() run and kick off the grace coroutine

            var controllerGo = new GameObject("Controller");
            spawned.Add(controllerGo);
            testUIBox.SetControllerDirectly(controllerGo.AddComponent<TestBaseController>());

            yield return null; // grace coroutine resumes and re-checks controller

            Assert.IsFalse(testUIBox.destroyQueued);
        }
        #endregion
    }
}
