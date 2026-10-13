using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using LowDefMustard.Control;

namespace LowDefMustard.UIBox.Tests.Editor
{
    public class UIBoxChoiceAndCursorTests
    {
        // State
        private readonly List<GameObject> spawned = new();

        // Setup
        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in spawned.Where(go => go != null))
            {
                Object.DestroyImmediate(go);
            }
            spawned.Clear();
        }

        #region PrivateMethods
        private TestUIBox CreateTestUIBox()
        {
            var go = new GameObject("TestUIBox");
            spawned.Add(go);
            return go.AddComponent<TestUIBox>();
        }
        #endregion

        #region ClearChoiceSelections
        [Test]
        public void ClearChoiceSelections_ClearsHighlightAndAllMarkers()
        {
            TestUIBox testUIBox = CreateTestUIBox();
            var first = new TestChoice();
            testUIBox.InjectChoiceOptions(new IUIChoice[] { first });
            testUIBox.SetHighlightedChoiceOption(first);
            first.Highlight(true);

            testUIBox.PublicClearChoiceSelections();

            Assert.IsNull(testUIBox.publicHighlightedChoiceOption);
            Assert.IsFalse(first.isHighlighted);
        }

        [Test]
        public void ClearChoiceSelections_ToleratesNullAndDeadEntriesInList()
        {
            TestUIBox testUIBox = CreateTestUIBox();
            var dead = new TestChoice { isAlive = false };
            dead.Highlight(true);
            testUIBox.InjectChoiceOptions(new IUIChoice[] { null, dead });

            Assert.DoesNotThrow(testUIBox.PublicClearChoiceSelections);
            Assert.IsTrue(dead.isHighlighted); // Dead choices are left alone
        }
        #endregion

        #region StandardChoose
        [Test]
        public void StandardChoose_NoHighlightedChoice_ReturnsFalse()
        {
            TestUIBox testUIBox = CreateTestUIBox();

            Assert.IsFalse(testUIBox.PublicStandardChoose(null));
        }

        [Test]
        public void StandardChoose_WithHighlightedChoice_InvokesItsUseChoice()
        {
            TestUIBox testUIBox = CreateTestUIBox();
            var choice = new TestChoice();
            testUIBox.SetHighlightedChoiceOption(choice);

            bool result = testUIBox.PublicStandardChoose(null);

            Assert.IsTrue(result);
            Assert.AreEqual(1, choice.useChoiceCount);
        }
        #endregion

        #region StandardMoveCursor / MoveCursor2D
        [Test]
        public void StandardMoveCursor_ChoiceUnavailable_ReturnsFalse()
        {
            TestUIBox testUIBox = CreateTestUIBox();
            var choice = new TestChoice();
            testUIBox.InjectChoiceOptions(new IUIChoice[] { choice });
            testUIBox.SetHighlightedChoiceOption(choice);
            testUIBox.PublicSetChoiceAvailable(false); // not available

            Assert.IsFalse(testUIBox.PublicStandardMoveCursor(ControllerInputType.NavigateRight, CursorMovementStyle.Combined));
        }

        [Test]
        public void StandardMoveCursor_AdvancesHighlightToNextOption()
        {
            TestUIBox testUIBox = CreateTestUIBox();
            var first = new TestChoice();
            var second = new TestChoice();
            testUIBox.InjectChoiceOptions(new IUIChoice[] { first, second });
            testUIBox.SetHighlightedChoiceOption(first);
            testUIBox.PublicSetChoiceAvailable(true);

            bool moved = testUIBox.PublicStandardMoveCursor(ControllerInputType.NavigateRight, CursorMovementStyle.Combined);

            Assert.IsTrue(moved);
            Assert.AreSame(second, testUIBox.publicHighlightedChoiceOption);
            Assert.IsTrue(second.isHighlighted);
            Assert.IsFalse(first.isHighlighted);
        }

        [Test]
        public void StandardMoveCursorSpatial_MovesToTheChoiceInThatDirection()
        {
            TestUIBox testUIBox = CreateTestUIBox();
            var origin = new TestChoice(new Vector2(0f, 0f), new Vector2(4f, 4f));
            var right = new TestChoice(new Vector2(10f, 0f), new Vector2(4f, 4f));
            var above = new TestChoice(new Vector2(0f, 10f), new Vector2(4f, 4f));
            testUIBox.InjectChoiceOptions(new IUIChoice[] { origin, right, above });
            testUIBox.SetHighlightedChoiceOption(origin);
            testUIBox.PublicSetChoiceAvailable(true);

            bool moved = testUIBox.PublicStandardMoveCursorSpatial(ControllerInputType.NavigateUp);

            Assert.IsTrue(moved);
            Assert.AreSame(above, testUIBox.publicHighlightedChoiceOption);
        }

        [Test]
        public void MoveCursor2D_WrapsAcrossFixedTwoColumnLayout()
        {
            TestUIBox testUIBox = CreateTestUIBox();
            var choices = new IUIChoice[] { new TestChoice(), new TestChoice(), new TestChoice(), new TestChoice(), new TestChoice() };
            testUIBox.InjectChoiceOptions(choices);
            testUIBox.SetHighlightedChoiceOption(choices[3]); // index 3 of 5 -- Down wraps (3+2>=5)
            testUIBox.PublicSetChoiceAvailable(true);

            bool moved = testUIBox.PublicMoveCursor2D(ControllerInputType.NavigateDown);

            Assert.IsTrue(moved);
            Assert.AreSame(choices[0], testUIBox.publicHighlightedChoiceOption);
        }
        #endregion

        #region TryEarlyExit
        [TestCase(ControllerInputType.Cancel)]
        [TestCase(ControllerInputType.Option)]
        [TestCase(ControllerInputType.Escape)]
        public void TryEarlyExit_ExitInput_QueuesDestroyAndReturnsTrue(ControllerInputType input)
        {
            TestUIBox testUIBox = CreateTestUIBox();

            bool result = testUIBox.PublicTryEarlyExit(input);

            Assert.IsTrue(result);
            Assert.IsTrue(testUIBox.destroyQueued);
        }

        [Test]
        public void TryEarlyExit_NonExitInput_ReturnsFalseAndDoesNotQueueDestroy()
        {
            TestUIBox testUIBox = CreateTestUIBox();

            bool result = testUIBox.PublicTryEarlyExit(ControllerInputType.Execute);

            Assert.IsFalse(result);
            Assert.IsFalse(testUIBox.destroyQueued);
        }

        [Test]
        public void TryEarlyExit_PreventEscapeOptionExitEnabled_AlwaysReturnsFalse()
        {
            TestUIBox testUIBox = CreateTestUIBox();
            testUIBox.publicPreventEscapeOptionExit = true;

            Assert.IsFalse(testUIBox.PublicTryEarlyExit(ControllerInputType.Cancel));
            Assert.IsFalse(testUIBox.destroyQueued);
        }
        #endregion

        #region TrySetController
        [Test]
        public void TrySetController_Null_ReturnsFalseAndLeavesControllerUnset()
        {
            TestUIBox testUIBox = CreateTestUIBox();

            Assert.IsFalse(testUIBox.TrySetController(null));
            Assert.IsNull(testUIBox.publicController);
        }

        [Test]
        public void TrySetController_ValidController_SetsControllerAndEnablesGlobalInput()
        {
            TestUIBox testUIBox = CreateTestUIBox();
            testUIBox.publicHandleGlobalInput = false;
            var controllerGo = new GameObject("Controller");
            spawned.Add(controllerGo);
            var controller = controllerGo.AddComponent<TestBaseController>();

            bool result = testUIBox.TrySetController(controller);

            Assert.IsTrue(result);
            Assert.AreSame(controller, testUIBox.publicController);
            Assert.IsTrue(testUIBox.publicHandleGlobalInput);
        }
        #endregion
    }
}
