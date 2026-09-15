using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using LowDefMustard.Control;

namespace LowDefMustard.UIBox.Tests
{
    public class UIBoxStateBehaviourDispatchTests
    {
        // State
        private readonly List<GameObject> spawned = new();

        // Setup
        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in spawned.Where(go => go != null))
            {
                Object.Destroy(go);
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
        public IEnumerator SetUpChoiceOptions_StateOverride_CalledDuringOnEnable()
        {
            TestUIBox testUIBox = CreateInactiveTestUIBox();
            bool overrideCalled = false;
            testUIBox.SetStateBehaviour(UIBoxState.Default, new UIBoxStateBehaviour(setupChoiceOptions: () => overrideCalled = true));

            testUIBox.gameObject.SetActive(true); // OnEnable calls SetUpChoiceOptions itself
            yield return null;

            Assert.IsTrue(overrideCalled);
        }

        [UnityTest]
        public IEnumerator ReconcileChoiceOptions_StateOverride_CalledInsteadOfStandard()
        {
            TestUIBox testUIBox = CreateInactiveTestUIBox();
            bool overrideCalled = false;
            testUIBox.SetStateBehaviour(UIBoxState.Default, new UIBoxStateBehaviour(reconcileChoiceOptions: () => overrideCalled = true));
            testUIBox.gameObject.SetActive(true);
            yield return null;

            testUIBox.PublicReconcileChoiceOptions();

            Assert.IsTrue(overrideCalled);
        }

        [UnityTest]
        public IEnumerator PrepareChooseAction_StateOverride_ReturnsOverrideResult()
        {
            TestUIBox testUIBox = CreateInactiveTestUIBox();
            testUIBox.SetStateBehaviour(UIBoxState.Default, new UIBoxStateBehaviour(prepareChooseAction: _ => true));
            testUIBox.gameObject.SetActive(true);
            yield return null;

            // NavigateRight would not normally satisfy StandardPrepareChooseAction (Execute-only)
            bool result = testUIBox.PublicPrepareChooseAction(ControllerInputType.NavigateRight);

            Assert.IsTrue(result);
        }

        [UnityTest]
        public IEnumerator Choose_StateOverride_ReturnsOverrideResult()
        {
            TestUIBox testUIBox = CreateInactiveTestUIBox();
            string receivedNodeId = null;
            testUIBox.SetStateBehaviour(UIBoxState.Default, new UIBoxStateBehaviour(choose: nodeId =>
            {
                receivedNodeId = nodeId;
                return true;
            }));
            testUIBox.gameObject.SetActive(true);
            yield return null;

            bool result = testUIBox.PublicChoose("some-node");

            Assert.IsTrue(result);
            Assert.AreEqual("some-node", receivedNodeId);
        }

        [UnityTest]
        public IEnumerator MoveCursor_StateOverride_ReturnsOverrideResult()
        {
            TestUIBox testUIBox = CreateInactiveTestUIBox();
            testUIBox.SetStateBehaviour(UIBoxState.Default, new UIBoxStateBehaviour(moveCursor: (_, _) => true));
            testUIBox.gameObject.SetActive(true);
            yield return null;

            // No choices injected - StandardMoveCursor would normally return false
            bool result = testUIBox.PublicMoveCursor(ControllerInputType.NavigateRight, CursorMovementStyle.Combined);

            Assert.IsTrue(result);
        }

        [UnityTest]
        public IEnumerator HandleGlobalInput_StateOverride_BypassesStandardEntirely()
        {
            TestUIBox testUIBox = CreateInactiveTestUIBox();
            bool overrideCalled = false;
            testUIBox.SetStateBehaviour(UIBoxState.Default, new UIBoxStateBehaviour(handleGlobalInput: _ =>
            {
                overrideCalled = true;
                return true;
            }));
            testUIBox.gameObject.SetActive(true);
            yield return null;

            // Cancel would normally hit TryEarlyExit and queue destroy
            testUIBox.GetInputHandler().Invoke(ControllerInputType.Cancel);

            Assert.IsTrue(overrideCalled);
            Assert.IsFalse(testUIBox.destroyQueued);
        }

        [UnityTest]
        public IEnumerator TryHandleBackNavigation_StateOverride_ConsumesBackInputBeforeEarlyExit()
        {
            TestUIBox testUIBox = CreateInactiveTestUIBox();
            bool backNavCalled = false;
            testUIBox.SetStateBehaviour(UIBoxState.Default, new UIBoxStateBehaviour(tryHandleBackNavigation: _ =>
            {
                backNavCalled = true;
                return true;
            }));
            testUIBox.gameObject.SetActive(true);
            yield return null;

            // isBackInput defaults to true for Cancel, so the override should fire and preempt TryEarlyExit
            testUIBox.GetInputHandler().Invoke(ControllerInputType.Cancel);

            Assert.IsTrue(backNavCalled);
            Assert.IsFalse(testUIBox.destroyQueued);
        }

        [UnityTest]
        public IEnumerator IsBackInput_StateOverriddenFalse_StillFallsThroughToEarlyExit()
        {
            TestUIBox testUIBox = CreateInactiveTestUIBox();
            bool backNavCalled = false;
            testUIBox.SetStateBehaviour(UIBoxState.Default, new UIBoxStateBehaviour(
                isBackInput: _ => false, // Cancel no longer counts as back input
                tryHandleBackNavigation: _ =>
                {
                    backNavCalled = true;
                    return true;
                }));
            testUIBox.gameObject.SetActive(true);
            yield return null;

            testUIBox.GetInputHandler().Invoke(ControllerInputType.Cancel);

            // isBackInput gates tryHandleBackNavigation - false means it's never even evaluated
            Assert.IsFalse(backNavCalled);
            Assert.IsTrue(testUIBox.destroyQueued);
        }
        #endregion
    }
}
