using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using LowDefMustard.Utils.Editor;

namespace LowDefMustard.Utils.Tests.Editor
{
    public class AutocompleteDropdownTests
    {
        // Const/Static Tunables
        private static readonly string[] _names = { "AveragePraise", "Bite", "BigSpitTake", "GentlePraise", "PraiseBand", "Wallop" };

        // State
        private EditorWindow window;

        #region Setup
        [TearDown]
        public void TearDown()
        {
            if (window != null) { window.Close(); }
        }
        #endregion

        #region PrivateMethods
        private (AutocompleteDropdown dropdown, TextField field) SetUpAttached(IEnumerable<string> options = null)
        {
            window = HeadlessEditorWindowTestHelper.CreateOffscreenWindow();
            var field = new TextField();
            var dropdown = new AutocompleteDropdown();
            window.rootVisualElement.Add(field);
            window.rootVisualElement.Add(dropdown);
            dropdown.Attach(field, options ?? _names);
            return (dropdown, field);
        }

        private static void Type(TextField field, string text)
        {
            string previous = field.text;
            field.SetValueWithoutNotify(text);
            using InputEvent inputEvent = InputEvent.GetPooled(previous, text);
            inputEvent.target = field;
            field.SendEvent(inputEvent);
        }

        private static void PressKey(VisualElement target, KeyCode keyCode)
        {
            using KeyDownEvent keyDownEvent = KeyDownEvent.GetPooled('\0', keyCode, EventModifiers.None);
            keyDownEvent.target = target;
            target.SendEvent(keyDownEvent);
        }

        private static bool IsShown(VisualElement dropdown) => dropdown.style.display.value == DisplayStyle.Flex;
        private static List<string> GetRowTexts(VisualElement dropdown) => dropdown.Query<Label>().ToList().Select(label => label.text).ToList();
        #endregion

        #region Tests
        [Test]
        public void Constructor_StartsHidden()
        {
            var dropdown = new AutocompleteDropdown();

            Assert.AreEqual(DisplayStyle.None, dropdown.style.display.value);
        }

        [Test]
        public void Attach_NullFieldOrOptions_DoesNotThrow()
        {
            var dropdown = new AutocompleteDropdown();

            Assert.DoesNotThrow(() => dropdown.Attach(null, _names));
            Assert.DoesNotThrow(() => dropdown.Attach(new TextField(), null));
        }

        [UnityTest]
        public IEnumerator Typing_MatchingText_ListsStartsWithFirst_ThenContains_IgnoringCase()
        {
            var (dropdown, field) = SetUpAttached();
            yield return null;

            Type(field, "praise");
            yield return null;

            Assert.IsTrue(IsShown(dropdown));
            CollectionAssert.AreEqual(new[] { "PraiseBand", "AveragePraise", "GentlePraise" }, GetRowTexts(dropdown));
        }

        [UnityTest]
        public IEnumerator Typing_DuplicateOptions_ListedOnce()
        {
            var (dropdown, field) = SetUpAttached(new[] { "Bite", "Bite", "Bitter" });
            yield return null;

            Type(field, "bit");
            yield return null;

            CollectionAssert.AreEqual(new[] { "Bite", "Bitter" }, GetRowTexts(dropdown));
        }

        [UnityTest]
        public IEnumerator Typing_LiveOptionsCollection_PicksUpLaterChanges()
        {
            var options = new List<string> { "Bite" };
            var (dropdown, field) = SetUpAttached(options);
            options.Add("Bitter");
            yield return null;

            Type(field, "bit");
            yield return null;

            CollectionAssert.AreEqual(new[] { "Bite", "Bitter" }, GetRowTexts(dropdown));
        }

        [UnityTest]
        public IEnumerator Typing_NoMatches_StaysHidden()
        {
            var (dropdown, field) = SetUpAttached();
            yield return null;

            Type(field, "zzz");
            yield return null;

            Assert.IsFalse(IsShown(dropdown));
        }

        [UnityTest]
        public IEnumerator Typing_ThenClearingText_Hides()
        {
            var (dropdown, field) = SetUpAttached();
            yield return null;

            Type(field, "bi");
            yield return null;
            Type(field, "  ");
            yield return null;

            Assert.IsFalse(IsShown(dropdown));
        }

        [UnityTest]
        public IEnumerator Typing_ManyMatches_LimitsToMaxSuggestions()
        {
            var (dropdown, field) = SetUpAttached(Enumerable.Range(0, 25).Select(i => $"Skill{i}"));
            yield return null;

            Type(field, "skill");
            yield return null;

            Assert.AreEqual(10, GetRowTexts(dropdown).Count);
        }

        [UnityTest]
        public IEnumerator DownThenEnter_ChoosesHighlightedSuggestion()
        {
            var (dropdown, field) = SetUpAttached();
            string changedTo = null;
            field.RegisterValueChangedCallback(changeEvent => changedTo = changeEvent.newValue);
            yield return null;

            Type(field, "praise");
            yield return null;
            PressKey(field, KeyCode.DownArrow);
            PressKey(field, KeyCode.DownArrow);
            PressKey(field, KeyCode.Return);
            yield return null;

            Assert.AreEqual("AveragePraise", field.value);
            Assert.AreEqual("AveragePraise", changedTo, "Choosing should commit like a typed entry (firing the change callback)");
            Assert.IsFalse(IsShown(dropdown));
        }

        [UnityTest]
        public IEnumerator Enter_WithNothingHighlighted_LeavesFieldText()
        {
            var (_, field) = SetUpAttached();
            yield return null;

            Type(field, "praise");
            yield return null;
            PressKey(field, KeyCode.Return);
            yield return null;

            Assert.AreEqual("praise", field.text);
        }

        [UnityTest]
        public IEnumerator Escape_Hides()
        {
            var (dropdown, field) = SetUpAttached();
            yield return null;

            Type(field, "bi");
            yield return null;
            PressKey(field, KeyCode.Escape);
            yield return null;

            Assert.IsFalse(IsShown(dropdown));
        }

        [UnityTest]
        public IEnumerator PointerDownOnRow_ChoosesThatSuggestion()
        {
            var (dropdown, field) = SetUpAttached();
            yield return null;

            Type(field, "bi");
            yield return null;
            Label secondRow = dropdown.Query<Label>().AtIndex(1);
            using (PointerDownEvent pointerDownEvent = PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown, button = 0 }))
            {
                pointerDownEvent.target = secondRow;
                secondRow.SendEvent(pointerDownEvent);
            }
            yield return null;

            Assert.AreEqual("BigSpitTake", field.value);
            Assert.IsFalse(IsShown(dropdown));
        }

        [UnityTest]
        public IEnumerator SharedDropdown_FollowsTheFieldBeingTyped()
        {
            var (dropdown, firstField) = SetUpAttached();
            var secondField = new TextField();
            window.rootVisualElement.Add(secondField);
            dropdown.Attach(secondField, _names);
            yield return null;

            Type(firstField, "praise");
            yield return null;
            Type(secondField, "wal");
            yield return null;
            PressKey(secondField, KeyCode.DownArrow);
            PressKey(secondField, KeyCode.Return);
            yield return null;

            Assert.AreEqual("Wallop", secondField.value);
            Assert.AreEqual("praise", firstField.text);
        }
        #endregion
    }
}
