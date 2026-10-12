using System.Collections;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace LowDefMustard.UIBox.Tests
{
    public class UIToolkitTextScanViewTests : PanelTestBase
    {
        #region Binding
        [UnityTest]
        public IEnumerator Bind_WithoutTextEntryContainer_WarnsAndStaysUnbound()
        {
            LogAssert.Expect(LogType.Warning, new Regex("UXML requires a TextEntryContainer"));
            UIToolkitTextScanView view = CreateView<UIToolkitTextScanView>(TestPanelAssets.emptyView);
            view.CreateTextEntry(TextEntryType.Simple);
            view.SetPointerInputEnabled(false);

            yield return WaitForLoadedUI(view);
            VisualElement root = GetLoadedRoot(view);

            Assert.IsNull(root.Q<TextEntryElement>());
            Assert.IsFalse(root.ClassListContains(USSClassNames.BoxView.pointerInputDisabled));
        }

        [UnityTest]
        public IEnumerator Bind_UILoaded_PlacesEntriesInTheirOwnContainerTypes()
        {
            UIToolkitTextScanView view = CreateView<UIToolkitTextScanView>(TestPanelAssets.textScanView);
            view.CreateTextEntry(TextEntryType.Simple);
            view.CreateChoiceEntry("Choice", null);

            yield return WaitForLoadedUI(view);
            VisualElement root = GetLoadedRoot(view);
            var textEntryContainer = root.Q<TextEntryContainer>();
            var choiceEntryContainer = root.Q<ChoiceEntryContainer>();

            Assert.AreEqual(1, textEntryContainer.childCount);
            Assert.IsInstanceOf<TextEntryElement>(textEntryContainer[0]);
            Assert.AreEqual(1, choiceEntryContainer.childCount);
            Assert.IsInstanceOf<ChoiceEntryElement>(choiceEntryContainer[0]);
        }
        #endregion

        #region ChoiceLayout
        [UnityTest]
        public IEnumerator Bind_NoChoiceLayoutSet_UsesHorizontal()
        {
            UIToolkitTextScanView view = CreateView<UIToolkitTextScanView>(TestPanelAssets.textScanView);

            yield return WaitForLoadedUI(view);
            var choiceEntryContainer = GetLoadedRoot(view).Q<ChoiceEntryContainer>();

            Assert.AreEqual(ChoiceLayout.Horizontal, choiceEntryContainer.choiceLayout);
            Assert.IsTrue(choiceEntryContainer.ClassListContains(USSClassNames.ChoiceEntries.horizontal));
        }

        [UnityTest]
        public IEnumerator SetChoiceLayout_BeforeUILoads_AppliedOnBind()
        {
            UIToolkitTextScanView view = CreateView<UIToolkitTextScanView>(TestPanelAssets.textScanView);
            view.SetChoiceLayout(ChoiceLayout.Vertical);

            yield return WaitForLoadedUI(view);
            var choiceEntryContainer = GetLoadedRoot(view).Q<ChoiceEntryContainer>();

            Assert.IsTrue(choiceEntryContainer.ClassListContains(USSClassNames.ChoiceEntries.vertical));
            Assert.IsFalse(choiceEntryContainer.ClassListContains(USSClassNames.ChoiceEntries.horizontal));
        }

        [UnityTest]
        public IEnumerator SetChoiceLayout_Bound_SwitchesContainerLayoutClasses()
        {
            UIToolkitTextScanView view = CreateView<UIToolkitTextScanView>(TestPanelAssets.textScanView);
            yield return WaitForLoadedUI(view);
            var choiceEntryContainer = GetLoadedRoot(view).Q<ChoiceEntryContainer>();

            view.SetChoiceLayout(ChoiceLayout.Vertical);
            Assert.IsTrue(choiceEntryContainer.ClassListContains(USSClassNames.ChoiceEntries.vertical));
            Assert.IsFalse(choiceEntryContainer.ClassListContains(USSClassNames.ChoiceEntries.horizontal));

            view.SetChoiceLayout(ChoiceLayout.Horizontal);
            Assert.IsTrue(choiceEntryContainer.ClassListContains(USSClassNames.ChoiceEntries.horizontal));
            Assert.IsFalse(choiceEntryContainer.ClassListContains(USSClassNames.ChoiceEntries.vertical));
        }
        #endregion
    }
}
