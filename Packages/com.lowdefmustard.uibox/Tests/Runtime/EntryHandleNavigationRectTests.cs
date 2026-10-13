using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace LowDefMustard.UIBox.Tests
{
    public class EntryHandleNavigationRectTests : PanelTestBase
    {
        // Test Notes:
        //  - the test style sheet gives choice rows a fixed 200 x 40 size and lays containers out as a row / column
        //  - navigation rects are panel space flipped to y up:  lower on screen = smaller y

        // Const Tunables
        private const float _rowWidth = 200f;
        private const float _rowHeight = 40f;
        private const float _tolerance = 0.5f;

        #region PrivateMethods
        private UIToolkitMenuView CreateStyledView() => CreateView<UIToolkitMenuView>(TestPanelAssets.menuView, TestPanelAssets.styleSheet);
        #endregion

        #region TryGetNavigationRect
        [UnityTest]
        public IEnumerator TryGetNavigationRect_BeforeUILoads_ReturnsFalse()
        {
            UIToolkitMenuView view = CreateStyledView();

            IUIChoice choice = view.CreateChoiceOption("Choice", null);

            Assert.IsFalse(choice.TryGetNavigationRect(out _));
            yield return WaitForLoadedUI(view);
            Assert.IsTrue(choice.TryGetNavigationRect(out _));
        }

        [UnityTest]
        public IEnumerator TryGetNavigationRect_LaidOut_ReturnsElementBoundsFlippedToYUp()
        {
            UIToolkitMenuView view = CreateStyledView();
            IUIChoice choice = view.CreateChoiceOption("Choice", null);

            yield return WaitForLoadedUI(view);
            Rect worldBound = GetLoadedRoot(view).Q<ChoiceEntryElement>().worldBound;

            Assert.IsTrue(choice.TryGetNavigationRect(out Rect navigationRect));
            Assert.AreEqual(_rowWidth, navigationRect.width, _tolerance);
            Assert.AreEqual(_rowHeight, navigationRect.height, _tolerance);
            Assert.AreEqual(worldBound.x, navigationRect.x, _tolerance);
            Assert.AreEqual(-worldBound.yMax, navigationRect.y, _tolerance);
        }

        [UnityTest]
        public IEnumerator TryGetNavigationRect_VerticalContainer_LaterEntriesAreLower()
        {
            UIToolkitMenuView view = CreateStyledView();
            IUIChoice firstChoice = view.CreateChoiceOption("First", null);
            IUIChoice secondChoice = view.CreateChoiceOption("Second", null);
            yield return WaitForLoadedUI(view);

            GetLoadedRoot(view).Q<ChoiceEntryContainer>().SetLayout(ChoiceLayout.Vertical);
            yield return Settle();

            Assert.IsTrue(firstChoice.TryGetNavigationRect(out Rect firstRect));
            Assert.IsTrue(secondChoice.TryGetNavigationRect(out Rect secondRect));
            Assert.AreEqual(firstRect.x, secondRect.x, _tolerance);
            Assert.LessOrEqual(secondRect.yMax, firstRect.y + _tolerance);
        }

        [UnityTest]
        public IEnumerator TryGetNavigationRect_HorizontalContainer_LaterEntriesAreFurtherRight()
        {
            UIToolkitMenuView view = CreateStyledView();
            IUIChoice firstChoice = view.CreateChoiceOption("First", null);
            IUIChoice secondChoice = view.CreateChoiceOption("Second", null);
            yield return WaitForLoadedUI(view);

            GetLoadedRoot(view).Q<ChoiceEntryContainer>().SetLayout(ChoiceLayout.Horizontal);
            yield return Settle();

            Assert.IsTrue(firstChoice.TryGetNavigationRect(out Rect firstRect));
            Assert.IsTrue(secondChoice.TryGetNavigationRect(out Rect secondRect));
            Assert.AreEqual(firstRect.y, secondRect.y, _tolerance);
            Assert.GreaterOrEqual(secondRect.x, firstRect.xMax - _tolerance);
        }

        [UnityTest]
        public IEnumerator TryGetNavigationRect_RemovedEntry_ReturnsFalse()
        {
            UIToolkitMenuView view = CreateStyledView();
            var choiceEntry = (ChoiceEntryHandle)view.CreateChoiceOption("Choice", null);
            yield return WaitForLoadedUI(view);
            Assert.IsTrue(choiceEntry.TryGetNavigationRect(out _));

            choiceEntry.Remove();

            Assert.IsFalse(choiceEntry.TryGetNavigationRect(out _));
        }
        #endregion
    }
}
