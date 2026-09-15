using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
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

        private UIChoiceButton CreateWiredButton(string name) => TestChoiceFactory.CreateWiredButton(name, spawned);
        #endregion

        #region ClearChoiceSelections
        [Test]
        public void ClearChoiceSelections_ClearsHighlightAndAllMarkers()
        {
            TestUIBox testUIBox = CreateTestUIBox();
            UIChoiceButton first = CreateWiredButton("First");
            var marker = new GameObject("Marker");
            spawned.Add(marker);
            var serializedObject = new SerializedObject(first);
            serializedObject.FindProperty("selectionMarker").objectReferenceValue = marker;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            testUIBox.InjectChoiceOptions(new UIChoice[] { first });
            testUIBox.SetHighlightedChoiceOption(first);
            first.Highlight(true);

            testUIBox.PublicClearChoiceSelections();

            Assert.IsNull(testUIBox.publicHighlightedChoiceOption);
            Assert.IsFalse(marker.activeSelf);
        }

        [Test]
        public void ClearChoiceSelections_ToleratesNullEntriesInList()
        {
            TestUIBox testUIBox = CreateTestUIBox();
            testUIBox.InjectChoiceOptions(new UIChoice[] { null });

            Assert.DoesNotThrow(testUIBox.PublicClearChoiceSelections);
        }
        #endregion

        #region FilterOutSubOptions
        [Test]
        public void FilterOutSubOptions_RemovesContainerChildrenButKeepsContainerAndStandaloneChoices()
        {
            UIChoiceButton subA = CreateWiredButton("SubA");
            UIChoiceButton subB = CreateWiredButton("SubB");
            var containerGo = new GameObject("Container", typeof(RectTransform));
            spawned.Add(containerGo);
            var container = containerGo.AddComponent<UIChoiceContainer>();
            container.itemHighlighted = new UnityEngine.Events.UnityEvent();
            container.Add(subA);
            container.Add(subB);
            UIChoiceButton standalone = CreateWiredButton("Standalone");

            var input = new List<UIChoice> { container, subA, subB, standalone };
            List<UIChoice> filtered = TestUIBox.PublicFilterOutSubOptions(input);

            CollectionAssert.AreEquivalent(new UIChoice[] { container, standalone }, filtered);
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
            UIChoiceButton choice = CreateWiredButton("Choice");
            bool clicked = false;
            choice.AddOnClickListener(() => clicked = true);
            testUIBox.SetHighlightedChoiceOption(choice);

            bool result = testUIBox.PublicStandardChoose(null);

            Assert.IsTrue(result);
            Assert.IsTrue(clicked);
        }
        #endregion

        #region StandardMoveCursor / MoveCursor2D
        [Test]
        public void StandardMoveCursor_ChoiceUnavailable_ReturnsFalse()
        {
            TestUIBox testUIBox = CreateTestUIBox();
            UIChoiceButton choice = CreateWiredButton("Choice");
            testUIBox.InjectChoiceOptions(new UIChoice[] { choice });
            testUIBox.SetHighlightedChoiceOption(choice);
            testUIBox.PublicSetChoiceAvailable(false); // not available

            Assert.IsFalse(testUIBox.PublicStandardMoveCursor(ControllerInputType.NavigateRight, CursorMovementStyle.Combined));
        }

        [Test]
        public void StandardMoveCursor_AdvancesHighlightToNextOption()
        {
            TestUIBox testUIBox = CreateTestUIBox();
            UIChoiceButton first = CreateWiredButton("First");
            UIChoiceButton second = CreateWiredButton("Second");
            testUIBox.InjectChoiceOptions(new UIChoice[] { first, second });
            testUIBox.SetHighlightedChoiceOption(first);
            testUIBox.PublicSetChoiceAvailable(true);

            bool moved = testUIBox.PublicStandardMoveCursor(ControllerInputType.NavigateRight, CursorMovementStyle.Combined);

            Assert.IsTrue(moved);
            Assert.AreSame(second, testUIBox.publicHighlightedChoiceOption);
        }

        [Test]
        public void MoveCursor2D_WrapsAcrossFixedTwoColumnLayout()
        {
            TestUIBox testUIBox = CreateTestUIBox();
            var choices = new[] { CreateWiredButton("A"), CreateWiredButton("B"), CreateWiredButton("C"), CreateWiredButton("D"), CreateWiredButton("E") };
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
