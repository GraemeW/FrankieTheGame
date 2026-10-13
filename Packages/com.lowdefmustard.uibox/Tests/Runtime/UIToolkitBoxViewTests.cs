using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace LowDefMustard.UIBox.Tests
{
    public class UIToolkitBoxViewTests : PanelTestBase
    {
        // Test Notes:
        //  - the base view is driven through UIToolkitMenuView (binds to any UXML) on a loaded panel
        //  - the menu UXML holds two ChoiceEntryContainers:  entries belong in the first

        #region PrivateMethods
        private static List<ChoiceEntryContainer> GetChoiceContainers(VisualElement root) => root.Query<ChoiceEntryContainer>().ToList();

        private static void PressAndReleasePointer(VisualElement target)
        {
            Vector2 position = target.worldBound.center;
            using (PointerDownEvent pointerDownEvent = PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown, mousePosition = position }))
            {
                pointerDownEvent.target = target;
                target.SendEvent(pointerDownEvent);
            }
            using (PointerUpEvent pointerUpEvent = PointerUpEvent.GetPooled(new Event { type = EventType.MouseUp, mousePosition = position }))
            {
                pointerUpEvent.target = target;
                target.SendEvent(pointerUpEvent);
            }
        }
        #endregion

        #region Binding
        [UnityTest]
        public IEnumerator Bind_UILoaded_PlacesEarlierEntriesInFirstContainerOfType()
        {
            UIToolkitMenuView view = CreateView<UIToolkitMenuView>(TestPanelAssets.menuView);
            view.CreateChoiceOption("First", null);
            view.CreateChoiceOption("Second", null);

            yield return WaitForLoadedUI(view);
            List<ChoiceEntryContainer> containers = GetChoiceContainers(GetLoadedRoot(view));

            Assert.AreEqual(2, containers.Count);
            Assert.AreEqual(2, containers[0].childCount);
            Assert.AreEqual(0, containers[1].childCount);
            Assert.AreEqual("First", GetChoiceLabel(containers[0][0]).text);
            Assert.AreEqual("Second", GetChoiceLabel(containers[0][1]).text);
        }

        [UnityTest]
        public IEnumerator Bind_UILoaded_RootIgnoresPicking()
        {
            UIToolkitMenuView view = CreateView<UIToolkitMenuView>(TestPanelAssets.menuView);

            yield return WaitForLoadedUI(view);

            Assert.AreEqual(PickingMode.Ignore, GetLoadedRoot(view).pickingMode);
        }

        [UnityTest]
        public IEnumerator Bind_StyleSheets_AppliedToRoot()
        {
            StyleSheet styleSheet = TestPanelAssets.styleSheet;
            UIToolkitMenuView view = CreateView<UIToolkitMenuView>(TestPanelAssets.menuView, styleSheet);
            view.CreateChoiceOption("Choice", null);

            yield return WaitForLoadedUI(view);
            VisualElement root = GetLoadedRoot(view);

            Assert.IsTrue(root.styleSheets.Contains(styleSheet));
            Assert.AreEqual(200f, root.Q<ChoiceEntryElement>().resolvedStyle.width, 0.5f); // The sheet's fixed row width
        }

        [UnityTest]
        public IEnumerator AddEntry_AfterBind_AttachesElementImmediately()
        {
            UIToolkitMenuView view = CreateView<UIToolkitMenuView>(TestPanelAssets.menuView);
            yield return WaitForLoadedUI(view);
            ChoiceEntryContainer container = GetChoiceContainers(GetLoadedRoot(view))[0];

            view.CreateChoiceOption("Choice", null);

            Assert.AreEqual(1, container.childCount);
            Assert.IsInstanceOf<ChoiceEntryElement>(container[0]);
        }

        [UnityTest]
        public IEnumerator Bind_MissingContainerType_WarnsAndLeavesEntryUsable()
        {
            LogAssert.Expect(LogType.Warning, new Regex("UXML has no ChoiceEntryContainer for entries"));
            int chosenCount = 0;
            UIToolkitMenuView view = CreateView<UIToolkitMenuView>(TestPanelAssets.emptyView);
            IUIChoice choice = view.CreateChoiceOption("Choice", () => chosenCount++);

            yield return WaitForLoadedUI(view);
            choice.UseChoice();

            Assert.IsNull(GetLoadedRoot(view).Q<ChoiceEntryElement>());
            Assert.IsFalse(choice.TryGetNavigationRect(out _));
            Assert.AreEqual(1, chosenCount);
        }

        [UnityTest]
        public IEnumerator Rebind_UIReloaded_ReattachesEntriesToTheNewUI()
        {
            int chosenCount = 0;
            UIToolkitMenuView view = CreateView<UIToolkitMenuView>(TestPanelAssets.menuView);
            IUIChoice choice = view.CreateChoiceOption("Choice", () => chosenCount++);
            choice.Highlight(true);
            yield return WaitForLoadedUI(view);
            VisualElement firstRoot = GetLoadedRoot(view);
            ChoiceEntryContainer firstContainer = GetChoiceContainers(firstRoot)[0];
            VisualElement firstElement = firstContainer[0];

            view.GetComponent<PanelRenderer>().visualTreeAsset = TestPanelAssets.textScanView;
            yield return WaitForLoadedUI(view, firstRoot);
            VisualElement reloadedRoot = GetLoadedRoot(view);
            ChoiceEntryContainer reloadedContainer = GetChoiceContainers(reloadedRoot)[0];

            Assert.AreNotSame(firstRoot, reloadedRoot);
            Assert.AreEqual(0, firstContainer.childCount);
            Assert.AreEqual(1, reloadedContainer.childCount);
            var reloadedElement = (ChoiceEntryElement)reloadedContainer[0];
            Assert.AreNotSame(firstElement, reloadedElement);
            Assert.AreEqual("Choice", GetChoiceLabel(reloadedElement).text);
            Assert.IsTrue(reloadedElement.ClassListContains(USSClassNames.ChoiceEntry.highlighted));

            Click(reloadedElement);
            yield return null;
            Assert.AreEqual(1, chosenCount);
        }

        [UnityTest]
        public IEnumerator Rebind_UIReloaded_ReappliesViewState()
        {
            int backExitCount = 0;
            UIToolkitMenuView view = CreateView<UIToolkitMenuView>(TestPanelAssets.menuView);
            yield return WaitForLoadedUI(view);
            VisualElement firstRoot = GetLoadedRoot(view);
            var model = new TestHeaderModel();
            view.SetVisible(false);
            view.SetPointerInputEnabled(false);
            view.SetDataSource(model);
            view.SetBackExitAction(() => backExitCount++);

            view.GetComponent<PanelRenderer>().visualTreeAsset = TestPanelAssets.textScanView;
            yield return WaitForLoadedUI(view, firstRoot);
            VisualElement reloadedRoot = GetLoadedRoot(view);
            var backExitButton = reloadedRoot.Q<BackExitButton>();

            Assert.AreEqual(Visibility.Hidden, reloadedRoot.resolvedStyle.visibility);
            Assert.IsTrue(reloadedRoot.ClassListContains(USSClassNames.BoxView.pointerInputDisabled));
            Assert.AreSame(model, reloadedRoot.dataSource);
            Assert.IsFalse(backExitButton.ClassListContains(USSClassNames.BackExit.hidden));

            Click(backExitButton);
            yield return null;
            Assert.AreEqual(1, backExitCount);
        }

        [UnityTest]
        public IEnumerator Reenable_WithoutUIChange_KeepsExistingBinding()
        {
            UIToolkitMenuView view = CreateView<UIToolkitMenuView>(TestPanelAssets.menuView);
            view.CreateChoiceOption("Choice", null);
            yield return WaitForLoadedUI(view);
            ChoiceEntryContainer container = GetChoiceContainers(GetLoadedRoot(view))[0];
            VisualElement element = container[0];

            view.enabled = false;
            view.enabled = true;
            yield return Settle();

            Assert.AreEqual(1, container.childCount);
            Assert.AreSame(element, container[0]);
        }
        #endregion

        #region Entries
        [UnityTest]
        public IEnumerator Remove_AttachedEntry_DetachesItsElement()
        {
            UIToolkitMenuView view = CreateView<UIToolkitMenuView>(TestPanelAssets.menuView);
            var removedEntry = (ChoiceEntryHandle)view.CreateChoiceOption("Removed", null);
            view.CreateChoiceOption("Kept", null);
            yield return WaitForLoadedUI(view);
            ChoiceEntryContainer container = GetChoiceContainers(GetLoadedRoot(view))[0];

            removedEntry.Remove();

            Assert.AreEqual(1, container.childCount);
            Assert.AreEqual("Kept", GetChoiceLabel(container[0]).text);
        }

        [UnityTest]
        public IEnumerator ClearEntries_AttachedEntries_DetachesEveryElement()
        {
            UIToolkitMenuView view = CreateView<UIToolkitMenuView>(TestPanelAssets.menuView);
            view.CreateChoiceOption("First", null);
            view.CreateChoiceSeparator(ChoiceSeparatorType.Major);
            view.CreateChoiceOption("Second", null);
            yield return WaitForLoadedUI(view);
            ChoiceEntryContainer container = GetChoiceContainers(GetLoadedRoot(view))[0];
            Assert.AreEqual(3, container.childCount);

            view.ClearEntries();

            Assert.AreEqual(0, container.childCount);
        }

        [UnityTest]
        public IEnumerator ChoiceEntryContainer_EmptyClass_FollowsItsEntries()
        {
            UIToolkitMenuView view = CreateView<UIToolkitMenuView>(TestPanelAssets.menuView);
            yield return WaitForLoadedUI(view);
            ChoiceEntryContainer container = GetChoiceContainers(GetLoadedRoot(view))[0];
            Assert.IsTrue(container.ClassListContains(USSClassNames.ChoiceEntries.empty));

            var choiceEntry = (ChoiceEntryHandle)view.CreateChoiceOption("Choice", null);
            Assert.IsFalse(container.ClassListContains(USSClassNames.ChoiceEntries.empty));

            choiceEntry.Remove();
            Assert.IsTrue(container.ClassListContains(USSClassNames.ChoiceEntries.empty));

            view.CreateChoiceOption("Choice", null);
            view.ClearEntries();
            Assert.IsTrue(container.ClassListContains(USSClassNames.ChoiceEntries.empty));
        }
        #endregion

        #region ViewState
        [UnityTest]
        public IEnumerator SetVisible_BeforeUILoads_AppliedOnBind()
        {
            UIToolkitMenuView view = CreateView<UIToolkitMenuView>(TestPanelAssets.menuView);
            view.SetVisible(false);

            yield return WaitForLoadedUI(view);

            Assert.AreEqual(Visibility.Hidden, GetLoadedRoot(view).resolvedStyle.visibility);
        }

        [UnityTest]
        public IEnumerator SetVisible_Bound_TogglesRootVisibility()
        {
            UIToolkitMenuView view = CreateView<UIToolkitMenuView>(TestPanelAssets.menuView);
            yield return WaitForLoadedUI(view);
            VisualElement root = GetLoadedRoot(view);
            Assert.AreEqual(Visibility.Visible, root.resolvedStyle.visibility);

            view.SetVisible(false);
            yield return Settle();
            Assert.AreEqual(Visibility.Hidden, root.resolvedStyle.visibility);

            view.SetVisible(true);
            yield return Settle();
            Assert.AreEqual(Visibility.Visible, root.resolvedStyle.visibility);
        }

        [UnityTest]
        public IEnumerator SetPointerInputEnabled_False_AddsClassAndStopsPointerDownAtRoot()
        {
            UIToolkitMenuView view = CreateView<UIToolkitMenuView>(TestPanelAssets.menuView);
            yield return WaitForLoadedUI(view);
            VisualElement root = GetLoadedRoot(view);
            int pointerDownCount = 0;
            var pointerTarget = new VisualElement();
            pointerTarget.RegisterCallback<PointerDownEvent>(_ => pointerDownCount++);
            root.Add(pointerTarget);
            yield return Settle();

            PressAndReleasePointer(pointerTarget);
            yield return null;
            Assert.IsFalse(root.ClassListContains(USSClassNames.BoxView.pointerInputDisabled));
            Assert.AreEqual(1, pointerDownCount);

            view.SetPointerInputEnabled(false);
            PressAndReleasePointer(pointerTarget);
            yield return null;
            Assert.IsTrue(root.ClassListContains(USSClassNames.BoxView.pointerInputDisabled));
            Assert.AreEqual(1, pointerDownCount);

            view.SetPointerInputEnabled(true);
            PressAndReleasePointer(pointerTarget);
            yield return null;
            Assert.IsFalse(root.ClassListContains(USSClassNames.BoxView.pointerInputDisabled));
            Assert.AreEqual(2, pointerDownCount);
        }

        [UnityTest]
        public IEnumerator SetDataSource_BeforeUILoads_BindsModelBoundElementsOnBind()
        {
            UIToolkitMenuView view = CreateView<UIToolkitMenuView>(TestPanelAssets.headerView);
            var model = new TestHeaderModel { headerText = "Header" };
            view.SetDataSource(model);

            yield return WaitForLoadedUI(view);
            VisualElement root = GetLoadedRoot(view);

            Assert.AreSame(model, root.dataSource);
            Assert.AreEqual("Header", root.Q<TestHeaderLabel>().text);
        }

        [UnityTest]
        public IEnumerator SetDataSource_Bound_BindsModelBoundElements()
        {
            UIToolkitMenuView view = CreateView<UIToolkitMenuView>(TestPanelAssets.headerView);
            yield return WaitForLoadedUI(view);
            VisualElement root = GetLoadedRoot(view);

            view.SetDataSource(new TestHeaderModel { headerText = "Header" });
            yield return Settle();

            Assert.AreEqual("Header", root.Q<TestHeaderLabel>().text);
        }
        #endregion

        #region BackExit
        [UnityTest]
        public IEnumerator BackExit_NoAction_IsHidden()
        {
            UIToolkitMenuView view = CreateView<UIToolkitMenuView>(TestPanelAssets.menuView);

            yield return WaitForLoadedUI(view);

            Assert.IsTrue(GetLoadedRoot(view).Q<BackExitButton>().ClassListContains(USSClassNames.BackExit.hidden));
        }

        [UnityTest]
        public IEnumerator BackExit_ActionSet_IsShownAndClickInvokesAction()
        {
            int backExitCount = 0;
            UIToolkitMenuView view = CreateView<UIToolkitMenuView>(TestPanelAssets.menuView);
            yield return WaitForLoadedUI(view);
            var backExitButton = GetLoadedRoot(view).Q<BackExitButton>();

            view.SetBackExitAction(() => backExitCount++);
            Click(backExitButton);
            yield return null;

            Assert.IsFalse(backExitButton.ClassListContains(USSClassNames.BackExit.hidden));
            Assert.AreEqual(1, backExitCount);
        }

        [UnityTest]
        public IEnumerator BackExit_ActionCleared_IsHiddenAndClickDoesNothing()
        {
            int backExitCount = 0;
            UIToolkitMenuView view = CreateView<UIToolkitMenuView>(TestPanelAssets.menuView);
            view.SetBackExitAction(() => backExitCount++);
            yield return WaitForLoadedUI(view);
            var backExitButton = GetLoadedRoot(view).Q<BackExitButton>();
            Assert.IsFalse(backExitButton.ClassListContains(USSClassNames.BackExit.hidden));

            view.SetBackExitAction(null);
            Click(backExitButton);
            yield return null;

            Assert.IsTrue(backExitButton.ClassListContains(USSClassNames.BackExit.hidden));
            Assert.AreEqual(0, backExitCount);
        }
        #endregion

        #region Nesting
        [UnityTest]
        public IEnumerator Bind_NotNested_HasNoNestedClass()
        {
            UIToolkitMenuView view = CreateView<UIToolkitMenuView>(TestPanelAssets.menuView);

            yield return WaitForLoadedUI(view);

            Assert.IsNull(view.GetComponent<PanelRenderer>().parentUI);
            Assert.IsFalse(GetLoadedRoot(view).ClassListContains(USSClassNames.BoxView.nested));
        }

        [UnityTest]
        public IEnumerator Bind_NestedUnderAnotherPanelRenderer_AddsNestedClass()
        {
            PanelRenderer parentPanelRenderer = CreatePanelRenderer("Parent", TestPanelAssets.emptyView);
            UIToolkitMenuView view = CreateView<UIToolkitMenuView>(TestPanelAssets.menuView, parent: parentPanelRenderer.transform);

            yield return WaitForLoadedUI(view);

            Assert.AreEqual(parentPanelRenderer, view.GetComponent<PanelRenderer>().parentUI);
            Assert.IsTrue(GetLoadedRoot(view).ClassListContains(USSClassNames.BoxView.nested));
        }
        #endregion
    }
}
