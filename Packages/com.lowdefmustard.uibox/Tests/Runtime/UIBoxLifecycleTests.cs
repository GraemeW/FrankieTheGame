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
            var go = new GameObject("TestUIBox");
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
        public IEnumerator Awake_DependenciesAcquired_TriggersAwake()
        {
            TestUIBox testUIBox = CreateInactiveTestUIBox();

            testUIBox.gameObject.SetActive(true);
            yield return null;

            Assert.AreEqual(1, testUIBox.awakeTriggeredCount);
        }

        [UnityTest]
        public IEnumerator OnEnable_KeepsChoiceOptionsByDefault_DroppingDeadOnes()
        {
            TestUIBox testUIBox = CreateInactiveTestUIBox();
            var choice = new TestChoice();
            var deadChoice = new TestChoice { isAlive = false };
            testUIBox.InjectChoiceOptions(new IUIChoice[] { choice, deadChoice });

            testUIBox.gameObject.SetActive(true);
            yield return null;

            Assert.AreEqual(1, testUIBox.enableTriggeredCount);
            CollectionAssert.AreEqual(new IUIChoice[] { choice }, testUIBox.publicChoiceOptions);
            Assert.IsTrue(testUIBox.PublicIsChoiceAvailable());
        }

        [UnityTest]
        public IEnumerator OnEnable_ClearChoiceOptionsOnEnable_ClearsThem()
        {
            TestUIBox testUIBox = CreateInactiveTestUIBox();
            testUIBox.publicClearChoiceOptionsOnEnable = true;
            testUIBox.InjectChoiceOptions(new IUIChoice[] { new TestChoice() });

            testUIBox.gameObject.SetActive(true);
            yield return null;

            CollectionAssert.IsEmpty(testUIBox.publicChoiceOptions);
            Assert.IsFalse(testUIBox.PublicIsChoiceAvailable());
        }

        [UnityTest]
        public IEnumerator OnDisable_ClearsHighlightAndTriggersDisable()
        {
            TestUIBox testUIBox = CreateInactiveTestUIBox();
            var choice = new TestChoice();
            testUIBox.gameObject.SetActive(true);
            yield return null;
            testUIBox.InjectChoiceOptions(new IUIChoice[] { choice });
            testUIBox.SetHighlightedChoiceOption(choice);
            choice.Highlight(true);

            testUIBox.gameObject.SetActive(false);

            Assert.AreEqual(1, testUIBox.disableTriggeredCount);
            Assert.IsNull(testUIBox.publicHighlightedChoiceOption);
            Assert.IsFalse(choice.isHighlighted);
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
