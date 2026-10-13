using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace LowDefMustard.UIBox.Tests
{
    public class TextScanElementTests : PanelTestBase
    {
        // Test Notes:
        //  - bound elements are created by their handles and attached through a view, so model -> element bindings run on a loaded panel
        //  - element state that involves no binding (classes, layout, entry type) is checked directly on a detached element

        #region PrivateMethods
        private UIToolkitTextScanView CreateTextScanView() => CreateView<UIToolkitTextScanView>(TestPanelAssets.textScanView);
        private static Label GetTextLabel(VisualElement textEntry) => textEntry.Q<Label>(className: USSClassNames.TextEntry.label);
        #endregion

        #region TextEntryElement
        [UnityTest]
        public IEnumerator TextEntry_NewEntry_IsHiddenUntilRevealed()
        {
            UIToolkitTextScanView view = CreateTextScanView();
            TextEntryHandle textEntry = view.CreateTextEntry(TextEntryType.Simple);
            yield return WaitForLoadedUI(view);
            var element = GetLoadedRoot(view).Q<TextEntryElement>();
            Assert.IsTrue(element.ClassListContains(USSClassNames.TextEntry.block));
            Assert.IsTrue(element.ClassListContains(USSClassNames.TextEntry.hidden));

            textEntry.Reveal();
            yield return Settle();

            Assert.IsFalse(element.ClassListContains(USSClassNames.TextEntry.hidden));
        }

        [UnityTest]
        public IEnumerator TextEntry_SetText_UpdatesLabel()
        {
            UIToolkitTextScanView view = CreateTextScanView();
            TextEntryHandle textEntry = view.CreateTextEntry(TextEntryType.Simple);
            textEntry.SetText("Before");
            yield return WaitForLoadedUI(view);
            Label label = GetTextLabel(GetLoadedRoot(view).Q<TextEntryElement>());
            Assert.AreEqual("Before", label.text);

            textEntry.SetText("After");
            yield return Settle();

            Assert.AreEqual("After", label.text);
        }

        [UnityTest]
        public IEnumerator TextEntry_Speech_HasBulletBeforeLabel()
        {
            UIToolkitTextScanView view = CreateTextScanView();
            view.CreateTextEntry(TextEntryType.Speech);

            yield return WaitForLoadedUI(view);
            var element = GetLoadedRoot(view).Q<TextEntryElement>();

            Assert.IsTrue(element.ClassListContains(USSClassNames.TextEntry.speech));
            Assert.AreEqual(2, element.childCount);
            Assert.IsTrue(element[0].ClassListContains(USSClassNames.TextEntry.bullet));
            Assert.IsTrue(element[1].ClassListContains(USSClassNames.TextEntry.label));
        }

        [Test]
        public void TextEntryElement_Simple_HasNoBullet()
        {
            var element = new TextEntryElement(TextEntryType.Simple);

            Assert.IsFalse(element.ClassListContains(USSClassNames.TextEntry.speech));
            Assert.AreEqual(1, element.childCount);
            Assert.IsNull(element.Q(className: USSClassNames.TextEntry.bullet));
        }

        [Test]
        public void TextEntryElement_EntryTypeChanged_AddsAndRemovesBullet()
        {
            var element = new TextEntryElement();

            element.entryType = TextEntryType.Speech;
            Assert.IsTrue(element.ClassListContains(USSClassNames.TextEntry.speech));
            Assert.IsTrue(element[0].ClassListContains(USSClassNames.TextEntry.bullet));

            element.entryType = TextEntryType.Simple;
            Assert.IsFalse(element.ClassListContains(USSClassNames.TextEntry.speech));
            Assert.IsNull(element.Q(className: USSClassNames.TextEntry.bullet));
        }
        #endregion

        #region ChoiceEntryElement
        [UnityTest]
        public IEnumerator ChoiceEntry_NewScanChoice_IsHiddenUntilRevealed()
        {
            UIToolkitTextScanView view = CreateTextScanView();
            ChoiceEntryHandle choiceEntry = view.CreateChoiceEntry("Choice", null);
            yield return WaitForLoadedUI(view);
            var element = GetLoadedRoot(view).Q<ChoiceEntryElement>();
            Assert.IsTrue(element.ClassListContains(USSClassNames.ChoiceEntry.block));
            Assert.IsTrue(element.ClassListContains(USSClassNames.ChoiceEntry.hidden));

            choiceEntry.Reveal();
            yield return Settle();

            Assert.IsFalse(element.ClassListContains(USSClassNames.ChoiceEntry.hidden));
        }

        [UnityTest]
        public IEnumerator ChoiceEntry_FixedChoiceOption_IsShownImmediately()
        {
            UIToolkitTextScanView view = CreateTextScanView();
            view.CreateChoiceOption("Choice", null);

            yield return WaitForLoadedUI(view);

            Assert.IsFalse(GetLoadedRoot(view).Q<ChoiceEntryElement>().ClassListContains(USSClassNames.ChoiceEntry.hidden));
        }

        [UnityTest]
        public IEnumerator ChoiceEntry_SetText_UpdatesLabel()
        {
            UIToolkitTextScanView view = CreateTextScanView();
            IUIChoice choice = view.CreateChoiceOption("Before", null);
            yield return WaitForLoadedUI(view);
            Label label = GetChoiceLabel(GetLoadedRoot(view).Q<ChoiceEntryElement>());
            Assert.AreEqual("Before", label.text);

            choice.SetText("After");
            yield return Settle();

            Assert.AreEqual("After", label.text);
        }

        [UnityTest]
        public IEnumerator ChoiceEntry_Highlight_TogglesHighlightedClass()
        {
            UIToolkitTextScanView view = CreateTextScanView();
            IUIChoice choice = view.CreateChoiceOption("Choice", null);
            yield return WaitForLoadedUI(view);
            var element = GetLoadedRoot(view).Q<ChoiceEntryElement>();
            Assert.IsFalse(element.ClassListContains(USSClassNames.ChoiceEntry.highlighted));

            choice.Highlight(true);
            yield return Settle();
            Assert.IsTrue(element.ClassListContains(USSClassNames.ChoiceEntry.highlighted));

            choice.Highlight(false);
            yield return Settle();
            Assert.IsFalse(element.ClassListContains(USSClassNames.ChoiceEntry.highlighted));
        }

        [UnityTest]
        public IEnumerator ChoiceEntry_Clicked_InvokesChooseAction()
        {
            int chosenCount = 0;
            UIToolkitTextScanView view = CreateTextScanView();
            view.CreateChoiceOption("Choice", () => chosenCount++);
            yield return WaitForLoadedUI(view);

            Click(GetLoadedRoot(view).Q<ChoiceEntryElement>());
            yield return null;

            Assert.AreEqual(1, chosenCount);
        }

        [Test]
        public void ChoiceEntryElement_New_IsNotFocusableAndHasMarkerThenLabel()
        {
            var element = new ChoiceEntryElement();

            Assert.IsFalse(element.focusable);
            Assert.IsTrue(element[0].ClassListContains(USSClassNames.ChoiceEntry.marker));
            Assert.IsTrue(element[1].ClassListContains(USSClassNames.ChoiceEntry.label));
        }
        #endregion

        #region ChoiceSeparatorElement
        [UnityTest]
        public IEnumerator ChoiceSeparator_CreatedThroughView_SitsBetweenChoicesWithItsTypeClass()
        {
            UIToolkitTextScanView view = CreateTextScanView();
            view.CreateChoiceOption("First", null);
            view.CreateChoiceSeparator(ChoiceSeparatorType.Major);
            view.CreateChoiceSeparator(ChoiceSeparatorType.Minor);
            view.CreateChoiceOption("Second", null);

            yield return WaitForLoadedUI(view);
            var container = GetLoadedRoot(view).Q<ChoiceEntryContainer>();
            List<ChoiceSeparatorElement> separators = container.Query<ChoiceSeparatorElement>().ToList();

            Assert.AreEqual(4, container.childCount);
            Assert.AreSame(separators[0], container[1]);
            Assert.AreSame(separators[1], container[2]);
            Assert.IsTrue(separators[0].ClassListContains(USSClassNames.ChoiceSeparator.block));
            Assert.IsFalse(separators[0].ClassListContains(USSClassNames.ChoiceSeparator.minor));
            Assert.IsTrue(separators[1].ClassListContains(USSClassNames.ChoiceSeparator.minor));
            Assert.AreEqual(PickingMode.Ignore, separators[0].pickingMode);
        }
        #endregion

        #region Containers
        [UnityTest]
        public IEnumerator Containers_LoadedFromUxml_HaveBlockClasses()
        {
            UIToolkitTextScanView view = CreateTextScanView();

            yield return WaitForLoadedUI(view);
            VisualElement root = GetLoadedRoot(view);

            Assert.IsTrue(root.Q<TextEntryContainer>().ClassListContains(USSClassNames.TextEntries.block));
            Assert.IsTrue(root.Q<ChoiceEntryContainer>().ClassListContains(USSClassNames.ChoiceEntries.block));
        }

        [UnityTest]
        public IEnumerator ChoiceEntryContainer_UxmlChoiceLayout_SetsLayoutClass()
        {
            UIToolkitMenuView view = CreateView<UIToolkitMenuView>(TestPanelAssets.menuView);

            yield return WaitForLoadedUI(view);
            List<ChoiceEntryContainer> containers = GetLoadedRoot(view).Query<ChoiceEntryContainer>().ToList();

            Assert.IsTrue(containers[0].ClassListContains(USSClassNames.ChoiceEntries.horizontal));
            Assert.AreEqual(ChoiceLayout.Vertical, containers[1].choiceLayout);
            Assert.IsTrue(containers[1].ClassListContains(USSClassNames.ChoiceEntries.vertical));
            Assert.IsFalse(containers[1].ClassListContains(USSClassNames.ChoiceEntries.horizontal));
        }

        [Test]
        public void ChoiceEntryContainer_SetLayout_SwitchesLayoutClasses()
        {
            var container = new ChoiceEntryContainer();
            Assert.IsTrue(container.ClassListContains(USSClassNames.ChoiceEntries.horizontal));

            container.SetLayout(ChoiceLayout.Vertical);

            Assert.AreEqual(ChoiceLayout.Vertical, container.choiceLayout);
            Assert.IsTrue(container.ClassListContains(USSClassNames.ChoiceEntries.vertical));
            Assert.IsFalse(container.ClassListContains(USSClassNames.ChoiceEntries.horizontal));
        }

        [Test]
        public void ChoiceEntryContainer_RefreshEmptyState_FollowsChildCount()
        {
            var container = new ChoiceEntryContainer();
            var child = new VisualElement();

            container.Add(child);
            container.RefreshEmptyState();
            Assert.IsFalse(container.ClassListContains(USSClassNames.ChoiceEntries.empty));

            child.RemoveFromHierarchy();
            container.RefreshEmptyState();
            Assert.IsTrue(container.ClassListContains(USSClassNames.ChoiceEntries.empty));
        }
        #endregion

        #region BackExitButton
        [Test]
        public void BackExitButton_New_IsNotFocusableAndShown()
        {
            var backExitButton = new BackExitButton();

            Assert.IsFalse(backExitButton.focusable);
            Assert.IsTrue(backExitButton.ClassListContains(USSClassNames.BackExit.block));
            Assert.IsFalse(backExitButton.ClassListContains(USSClassNames.BackExit.hidden));
        }

        [Test]
        public void BackExitButton_SetShown_TogglesHiddenClass()
        {
            var backExitButton = new BackExitButton();

            backExitButton.SetShown(false);
            Assert.IsTrue(backExitButton.ClassListContains(USSClassNames.BackExit.hidden));

            backExitButton.SetShown(true);
            Assert.IsFalse(backExitButton.ClassListContains(USSClassNames.BackExit.hidden));
        }
        #endregion
    }
}
