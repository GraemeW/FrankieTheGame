using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace LowDefMustard.UIBox.Tests
{
    public class MenuElementTests : PanelTestBase
    {
        // Test Notes:
        //  - bound elements are created by their handles and attached through a view, so model -> element bindings run on a loaded panel
        //  - a slider drag is stood in for by setting the inner Slider's value:  the same change event a drag raises, without depending on themed slider layout

        #region PrivateMethods
        private UIToolkitMenuView CreateMenuView() => CreateView<UIToolkitMenuView>(TestPanelAssets.menuView);
        #endregion

        #region SliderChoiceElement
        [UnityTest]
        public IEnumerator SliderChoice_BoundToHandle_ShowsTextValueAndHighlight()
        {
            UIToolkitMenuView view = CreateMenuView();
            var sliderChoice = new SliderChoiceHandle(view, "Volume", 0.5f, 0.1f, null);
            view.AddEntry(sliderChoice);
            yield return WaitForLoadedUI(view);
            var element = GetLoadedRoot(view).Q<SliderChoiceElement>();
            Assert.IsTrue(element.ClassListContains(USSClassNames.ChoiceEntry.block));
            Assert.IsTrue(element.ClassListContains(USSClassNames.SliderChoice.block));
            Assert.AreEqual("Volume", GetChoiceLabel(element).text);
            Assert.AreEqual(0.5f, element.Q<Slider>().value, 0.001f);
            Assert.IsFalse(element.ClassListContains(USSClassNames.ChoiceEntry.highlighted));

            sliderChoice.SetText("Music");
            sliderChoice.Highlight(true);
            yield return Settle();

            Assert.AreEqual("Music", GetChoiceLabel(element).text);
            Assert.IsTrue(element.ClassListContains(USSClassNames.ChoiceEntry.highlighted));
        }

        [UnityTest]
        public IEnumerator SliderChoice_SliderValueChanged_SetsHandleValueAndNotifies()
        {
            var notifiedValues = new List<float>();
            UIToolkitMenuView view = CreateMenuView();
            var sliderChoice = new SliderChoiceHandle(view, "Volume", 0.5f, 0.1f, notifiedValues.Add);
            view.AddEntry(sliderChoice);
            yield return WaitForLoadedUI(view);
            var slider = GetLoadedRoot(view).Q<SliderChoiceElement>().Q<Slider>();

            slider.value = 0.75f;
            yield return Settle();

            Assert.AreEqual(0.75f, sliderChoice.value, 0.001f);
            Assert.AreEqual(1, notifiedValues.Count);
            Assert.AreEqual(0.75f, notifiedValues[0], 0.001f);
            Assert.AreEqual(0.75f, slider.value, 0.001f);
        }

        [UnityTest]
        public IEnumerator SliderChoice_HandleValueSet_UpdatesSliderWithoutNotifyingAgain()
        {
            int notifiedCount = 0;
            UIToolkitMenuView view = CreateMenuView();
            var sliderChoice = new SliderChoiceHandle(view, "Volume", 0.5f, 0.1f, _ => notifiedCount++);
            view.AddEntry(sliderChoice);
            yield return WaitForLoadedUI(view);
            var slider = GetLoadedRoot(view).Q<SliderChoiceElement>().Q<Slider>();

            sliderChoice.SetValue(0.25f);
            yield return Settle();

            Assert.AreEqual(0.25f, slider.value, 0.001f);
            Assert.AreEqual(1, notifiedCount);
        }

        [Test]
        public void SliderChoiceElement_New_SliderIsNotFocusable()
        {
            var element = new SliderChoiceElement();

            Assert.IsFalse(element.Q<Slider>().focusable);
            Assert.IsTrue(element.Q<Slider>().ClassListContains(USSClassNames.SliderChoice.slider));
        }
        #endregion

        #region ToggleChoiceElement
        [UnityTest]
        public IEnumerator ToggleChoice_BoundToHandle_ShowsTextOnStateAndHighlight()
        {
            UIToolkitMenuView view = CreateMenuView();
            var toggleChoice = new ToggleChoiceHandle(view, "Fullscreen", true, null);
            view.AddEntry(toggleChoice);
            yield return WaitForLoadedUI(view);
            var element = GetLoadedRoot(view).Q<ToggleChoiceElement>();
            Assert.IsTrue(element.ClassListContains(USSClassNames.ChoiceEntry.block));
            Assert.IsTrue(element.ClassListContains(USSClassNames.ToggleChoice.block));
            Assert.AreEqual("Fullscreen", GetChoiceLabel(element).text);
            Assert.IsTrue(element.ClassListContains(USSClassNames.ToggleChoice.on));

            toggleChoice.SetValueWithoutNotify(false);
            toggleChoice.SetText("Windowed");
            toggleChoice.Highlight(true);
            yield return Settle();

            Assert.IsFalse(element.ClassListContains(USSClassNames.ToggleChoice.on));
            Assert.AreEqual("Windowed", GetChoiceLabel(element).text);
            Assert.IsTrue(element.ClassListContains(USSClassNames.ChoiceEntry.highlighted));
        }

        [UnityTest]
        public IEnumerator ToggleChoice_Clicked_TogglesAndNotifies()
        {
            var notifiedValues = new List<bool>();
            UIToolkitMenuView view = CreateMenuView();
            var toggleChoice = new ToggleChoiceHandle(view, "Fullscreen", false, notifiedValues.Add);
            view.AddEntry(toggleChoice);
            yield return WaitForLoadedUI(view);
            var element = GetLoadedRoot(view).Q<ToggleChoiceElement>();

            Click(element);
            yield return Settle();

            Assert.IsTrue(toggleChoice.isOn);
            Assert.IsTrue(element.ClassListContains(USSClassNames.ToggleChoice.on));
            CollectionAssert.AreEqual(new[] { true }, notifiedValues);
        }

        [Test]
        public void ToggleChoiceElement_New_IsNotFocusableAndHasCheckInsideBox()
        {
            var element = new ToggleChoiceElement();

            Assert.IsFalse(element.focusable);
            VisualElement box = element.Q(className: USSClassNames.ToggleChoice.box);
            Assert.IsNotNull(box);
            Assert.IsTrue(box[0].ClassListContains(USSClassNames.ToggleChoice.check));
        }
        #endregion

        #region ChoiceGroupElement
        [UnityTest]
        public IEnumerator ChoiceGroup_Attached_HoldsSubChoiceElementsInOrder()
        {
            UIToolkitMenuView view = CreateMenuView();
            var choiceGroup = new ChoiceGroupHandle(view, typeof(ChoiceEntryContainer));
            choiceGroup.AddSubChoice("Yes", null);
            view.AddEntry(choiceGroup);
            yield return WaitForLoadedUI(view);
            var element = GetLoadedRoot(view).Q<ChoiceGroupElement>();

            choiceGroup.AddSubChoice("No", null);
            yield return Settle();

            Assert.IsTrue(element.ClassListContains(USSClassNames.ChoiceGroup.block));
            Assert.AreEqual(PickingMode.Ignore, element.pickingMode);
            Assert.AreEqual(2, element.childCount);
            Assert.AreEqual("Yes", GetChoiceLabel(element[0]).text);
            Assert.AreEqual("No", GetChoiceLabel(element[1]).text);
        }

        [UnityTest]
        public IEnumerator ChoiceGroup_Highlight_HighlightsFirstSubChoiceElement()
        {
            UIToolkitMenuView view = CreateMenuView();
            var choiceGroup = new ChoiceGroupHandle(view, typeof(ChoiceEntryContainer));
            choiceGroup.AddSubChoice("Yes", null);
            choiceGroup.AddSubChoice("No", null);
            view.AddEntry(choiceGroup);
            yield return WaitForLoadedUI(view);
            var element = GetLoadedRoot(view).Q<ChoiceGroupElement>();

            choiceGroup.Highlight(true);
            yield return Settle();

            Assert.IsTrue(element[0].ClassListContains(USSClassNames.ChoiceEntry.highlighted));
            Assert.IsFalse(element[1].ClassListContains(USSClassNames.ChoiceEntry.highlighted));
        }

        [UnityTest]
        public IEnumerator ChoiceGroup_SubChoiceClicked_InvokesOnlyItsAction()
        {
            int yesCount = 0;
            int noCount = 0;
            UIToolkitMenuView view = CreateMenuView();
            var choiceGroup = new ChoiceGroupHandle(view, typeof(ChoiceEntryContainer));
            choiceGroup.AddSubChoice("Yes", () => yesCount++);
            choiceGroup.AddSubChoice("No", () => noCount++);
            view.AddEntry(choiceGroup);
            yield return WaitForLoadedUI(view);
            var element = GetLoadedRoot(view).Q<ChoiceGroupElement>();

            Click((ChoiceEntryElement)element[1]);
            yield return null;

            Assert.AreEqual(0, yesCount);
            Assert.AreEqual(1, noCount);
        }

        [UnityTest]
        public IEnumerator ChoiceGroup_Removed_DetachesGroupAndSubChoiceElements()
        {
            UIToolkitMenuView view = CreateMenuView();
            var choiceGroup = new ChoiceGroupHandle(view, typeof(ChoiceEntryContainer));
            choiceGroup.AddSubChoice("Yes", null);
            view.AddEntry(choiceGroup);
            yield return WaitForLoadedUI(view);
            VisualElement root = GetLoadedRoot(view);
            var element = root.Q<ChoiceGroupElement>();

            choiceGroup.Remove();

            Assert.IsNull(root.Q<ChoiceGroupElement>());
            Assert.AreEqual(0, element.childCount);
        }
        #endregion

        #region ModelBoundLabel
        [UnityTest]
        public IEnumerator ModelBoundLabel_ModelText_OverridesAuthoredText()
        {
            UIToolkitMenuView view = CreateView<UIToolkitMenuView>(TestPanelAssets.headerView);
            var model = new TestHeaderModel { headerText = "Header" };
            view.SetDataSource(model);
            yield return WaitForLoadedUI(view);
            var label = GetLoadedRoot(view).Q<TestHeaderLabel>();
            Assert.IsTrue(label.ClassListContains(USSClassNames.ModelBoundLabel.block));
            Assert.AreEqual("Header", label.text);

            model.headerText = "Changed";
            yield return Settle();

            Assert.AreEqual("Changed", label.text);
        }

        [UnityTest]
        public IEnumerator ModelBoundLabel_EmptyModelText_TogglesEmptyClass()
        {
            UIToolkitMenuView view = CreateView<UIToolkitMenuView>(TestPanelAssets.headerView);
            var model = new TestHeaderModel();
            view.SetDataSource(model);
            yield return WaitForLoadedUI(view);
            var label = GetLoadedRoot(view).Q<TestHeaderLabel>();
            Assert.AreEqual("", label.text);
            Assert.IsTrue(label.ClassListContains(USSClassNames.ModelBoundLabel.empty));

            model.headerText = "Header";
            yield return Settle();
            Assert.IsFalse(label.ClassListContains(USSClassNames.ModelBoundLabel.empty));

            model.headerText = "";
            yield return Settle();
            Assert.IsTrue(label.ClassListContains(USSClassNames.ModelBoundLabel.empty));
        }
        #endregion
    }
}
