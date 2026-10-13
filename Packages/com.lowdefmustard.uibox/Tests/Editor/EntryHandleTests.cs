using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using LowDefMustard.Control;

namespace LowDefMustard.UIBox.Tests.Editor
{
    public class EntryHandleTests
    {
        // Test Notes:
        //  - handles are driven without a loaded panel:  the view never binds, so no elements exist
        //  - what is under test is the handle logic (choose / highlight / value / removal) and the state it writes to its model
        
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
        private UIToolkitMenuView CreateView()
        {
            var go = new GameObject("View");
            spawned.Add(go);
            return go.AddComponent<UIToolkitMenuView>();
        }
        #endregion

        #region ChoiceEntryHandle
        [Test]
        public void ChoiceEntry_UseChoice_InvokesChooseAction()
        {
            int chosenCount = 0;
            var choiceEntry = new ChoiceEntryHandle(CreateView(), "Choice", true, () => chosenCount++);

            choiceEntry.UseChoice();

            Assert.AreEqual(1, chosenCount);
        }

        [Test]
        public void ChoiceEntry_Highlight_SetsStateAndInvokesHighlightActionOnlyWhenEnabling()
        {
            int highlightedCount = 0;
            var choiceEntry = new ChoiceEntryHandle(CreateView(), "Choice", true, null, () => highlightedCount++);

            choiceEntry.Highlight(true);
            Assert.IsTrue(choiceEntry.isHighlighted);
            Assert.AreEqual(1, highlightedCount);

            choiceEntry.Highlight(false);
            Assert.IsFalse(choiceEntry.isHighlighted);
            Assert.AreEqual(1, highlightedCount);
        }

        [Test]
        public void ChoiceEntry_SetTextAndReveal_UpdateTheModel()
        {
            var choiceEntry = new ChoiceEntryHandle(CreateView(), "Before", false, null);
            Assert.IsFalse(choiceEntry.isRevealed);

            choiceEntry.SetText("After");
            choiceEntry.Reveal();

            Assert.AreEqual("After", choiceEntry.text);
            Assert.IsTrue(choiceEntry.isRevealed);
        }

        [Test]
        public void ChoiceEntry_Removed_IsDeadAndIgnoresChooseAndHighlightActions()
        {
            int chosenCount = 0;
            int highlightedCount = 0;
            UIToolkitMenuView view = CreateView();
            var choiceEntry = new ChoiceEntryHandle(view, "Choice", true, () => chosenCount++, () => highlightedCount++);
            view.AddEntry(choiceEntry);

            choiceEntry.Remove();
            choiceEntry.UseChoice();
            choiceEntry.Highlight(true);

            Assert.IsFalse(choiceEntry.isAlive);
            Assert.AreEqual(0, chosenCount);
            Assert.AreEqual(0, highlightedCount);
        }

        [Test]
        public void EntryHandle_UnplacedEntry_HasNoNavigationRect()
        {
            var choiceEntry = new ChoiceEntryHandle(CreateView(), "Choice", true, null);

            Assert.IsFalse(choiceEntry.TryGetNavigationRect(out _));
        }
        #endregion

        #region View
        [Test]
        public void View_ClearEntries_KillsEveryEntry()
        {
            UIToolkitMenuView view = CreateView();
            var first = new ChoiceEntryHandle(view, "First", true, null);
            var second = new ChoiceEntryHandle(view, "Second", true, null);
            view.AddEntry(first);
            view.AddEntry(second);

            view.ClearEntries();

            Assert.IsFalse(first.isAlive);
            Assert.IsFalse(second.isAlive);
        }

        [Test]
        public void View_CreateChoiceOption_ReturnsLiveChoiceWiredToItsActions()
        {
            int chosenCount = 0;
            int highlightedCount = 0;

            IUIChoice choice = CreateView().CreateChoiceOption("Choice", () => chosenCount++, () => highlightedCount++);
            choice.Highlight(true);
            choice.UseChoice();

            Assert.IsTrue(choice.isAlive);
            Assert.AreEqual(1, chosenCount);
            Assert.AreEqual(1, highlightedCount);
        }
        #endregion

        #region Box + View
        [Test]
        public void Box_AddNonDestroyChoiceOption_AddsViewChoiceThatRunsActionAndRaisesItemSelected()
        {
            var go = new GameObject("Box");
            spawned.Add(go);
            go.AddComponent<UIToolkitMenuView>();
            var testUIBox = go.AddComponent<TestUIBox>();
            int chosenCount = 0;
            int itemSelectedCount = 0;
            testUIBox.SubscribeToReceiverUpdates(true, (type, _) =>
            {
                if (type == ReceiverModifiedType.ItemSelected) { itemSelectedCount++; }
            });

            IUIChoice choice = testUIBox.PublicAddNonDestroyChoiceOption("Choice", () => chosenCount++);
            choice.UseChoice();

            CollectionAssert.Contains(testUIBox.publicChoiceOptions, choice);
            Assert.AreEqual(1, chosenCount);
            Assert.AreEqual(1, itemSelectedCount);
            Assert.IsFalse(go == null); // Not destroyed on choose
        }

        [Test]
        public void Box_AddChoiceOption_WithoutView_ReturnsNullAndAddsNothing()
        {
            var go = new GameObject("Box");
            spawned.Add(go);
            var testUIBox = go.AddComponent<TestUIBox>();

            IUIChoice choice = testUIBox.PublicAddChoiceOption("Choice", null);

            Assert.IsNull(choice);
            CollectionAssert.IsEmpty(testUIBox.publicChoiceOptions);
        }
        #endregion

        #region SliderChoiceHandle
        [Test]
        public void Slider_TryMoveLeftRight_StepsValueAndNotifies_WithoutBeingAHighlightMove()
        {
            var notifiedValues = new List<float>();
            var slider = new SliderChoiceHandle(CreateView(), "Volume", 0.5f, 0.1f, notifiedValues.Add);

            bool movedRight = slider.TryMove(ControllerInputType.NavigateRight, out bool isHighlightMoveRight);
            bool movedLeft = slider.TryMove(ControllerInputType.NavigateLeft, out bool isHighlightMoveLeft);

            Assert.IsTrue(movedRight);
            Assert.IsTrue(movedLeft);
            Assert.IsFalse(isHighlightMoveRight);
            Assert.IsFalse(isHighlightMoveLeft);
            Assert.AreEqual(2, notifiedValues.Count);
            Assert.AreEqual(0.6f, notifiedValues[0], 0.0001f);
            Assert.AreEqual(0.5f, slider.value, 0.0001f);
        }

        [Test]
        public void Slider_TryMoveUpDown_IsNotIntercepted()
        {
            var slider = new SliderChoiceHandle(CreateView(), "Volume", 0.5f, 0.1f, null);

            Assert.IsFalse(slider.TryMove(ControllerInputType.NavigateUp, out _));
            Assert.IsFalse(slider.TryMove(ControllerInputType.NavigateDown, out _));
            Assert.AreEqual(0.5f, slider.value, 0.0001f);
        }

        [Test]
        public void Slider_ValueIsClamped_AndAnUnchangedValueDoesNotNotify()
        {
            int notifiedCount = 0;
            var slider = new SliderChoiceHandle(CreateView(), "Volume", 2f, 0.25f, _ => notifiedCount++);
            Assert.AreEqual(1f, slider.value); // Clamped on construction

            slider.TryMove(ControllerInputType.NavigateRight, out _); // Already at the top
            Assert.AreEqual(0, notifiedCount);

            slider.SetValue(-3f);
            Assert.AreEqual(0f, slider.value);
            Assert.AreEqual(1, notifiedCount);
        }
        #endregion

        #region ToggleChoiceHandle
        [Test]
        public void Toggle_UseChoice_FlipsAndNotifies()
        {
            var notifiedValues = new List<bool>();
            var toggle = new ToggleChoiceHandle(CreateView(), "Full Screen", false, notifiedValues.Add);

            toggle.UseChoice();
            toggle.UseChoice();

            CollectionAssert.AreEqual(new[] { true, false }, notifiedValues);
            Assert.IsFalse(toggle.isOn);
        }

        [Test]
        public void Toggle_SetValueWithoutNotify_IsSilent_AndAnUnchangedValueDoesNotNotify()
        {
            int notifiedCount = 0;
            var toggle = new ToggleChoiceHandle(CreateView(), "Full Screen", false, _ => notifiedCount++);

            toggle.SetValueWithoutNotify(true);
            Assert.IsTrue(toggle.isOn);
            Assert.AreEqual(0, notifiedCount);

            toggle.SetValue(true);
            Assert.AreEqual(0, notifiedCount);
        }
        #endregion

        #region ChoiceGroupHandle
        [Test]
        public void Group_Highlight_HighlightsFirstSubChoice_AndClearsOnUnhighlight()
        {
            var group = new ChoiceGroupHandle(CreateView(), typeof(ChoiceEntryContainer));
            var first = (ChoiceEntryHandle)group.AddSubChoice("First", null);
            var second = (ChoiceEntryHandle)group.AddSubChoice("Second", null);

            group.Highlight(true);
            Assert.IsTrue(first.isHighlighted);
            Assert.IsFalse(second.isHighlighted);

            group.Highlight(false);
            Assert.IsFalse(first.isHighlighted);
        }

        [Test]
        public void Group_TryMoveLeftRight_MovesInnerHighlightWithWraparound_AsAHighlightMove()
        {
            var group = new ChoiceGroupHandle(CreateView(), typeof(ChoiceEntryContainer));
            var first = (ChoiceEntryHandle)group.AddSubChoice("First", null);
            var second = (ChoiceEntryHandle)group.AddSubChoice("Second", null);
            group.Highlight(true);

            bool movedRight = group.TryMove(ControllerInputType.NavigateRight, out bool isHighlightMove);
            Assert.IsTrue(movedRight);
            Assert.IsTrue(isHighlightMove);
            Assert.IsTrue(second.isHighlighted);
            Assert.IsFalse(first.isHighlighted);

            group.TryMove(ControllerInputType.NavigateRight, out _); // Wraps back to the first
            Assert.IsTrue(first.isHighlighted);

            group.TryMove(ControllerInputType.NavigateLeft, out _); // Wraps round to the last
            Assert.IsTrue(second.isHighlighted);
        }

        [Test]
        public void Group_TryMoveUpDown_OrWithNoSubChoices_IsNotIntercepted()
        {
            var emptyGroup = new ChoiceGroupHandle(CreateView(), typeof(ChoiceEntryContainer));
            var group = new ChoiceGroupHandle(CreateView(), typeof(ChoiceEntryContainer));
            group.AddSubChoice("Only", null);
            group.Highlight(true);

            Assert.IsFalse(emptyGroup.TryMove(ControllerInputType.NavigateRight, out _));
            Assert.IsFalse(group.TryMove(ControllerInputType.NavigateUp, out _));
            Assert.IsFalse(group.TryMove(ControllerInputType.NavigateDown, out _));
        }

        [Test]
        public void Group_UseChoice_UsesTheHighlightedSubChoiceOnly()
        {
            int firstChosenCount = 0;
            int secondChosenCount = 0;
            var group = new ChoiceGroupHandle(CreateView(), typeof(ChoiceEntryContainer));
            group.AddSubChoice("First", () => firstChosenCount++);
            group.AddSubChoice("Second", () => secondChosenCount++);

            group.UseChoice(); // Nothing highlighted yet
            Assert.AreEqual(0, firstChosenCount + secondChosenCount);

            group.Highlight(true);
            group.TryMove(ControllerInputType.NavigateRight, out _);
            group.UseChoice();

            Assert.AreEqual(0, firstChosenCount);
            Assert.AreEqual(1, secondChosenCount);
        }
        #endregion
    }
}
