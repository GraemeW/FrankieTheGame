using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using LowDefMustard.Control;

namespace LowDefMustard.UIBox.Tests.Editor
{
    public class UIChoiceContainerTests
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
        private UIChoiceButton CreateWiredButton(string name) => TestChoiceFactory.CreateWiredButton(name, spawned);

        private UIChoiceContainer CreateContainer(bool isMoveHorizontal, params UIChoice[] subOptions)
        {
            var go = new GameObject("Container");
            spawned.Add(go);
            var container = go.AddComponent<UIChoiceContainer>();
            container.itemHighlighted = new UnityEngine.Events.UnityEvent();

            var serializedObject = new SerializedObject(container);
            serializedObject.FindProperty("isMoveHorizontal").boolValue = isMoveHorizontal;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            foreach (UIChoice subOption in subOptions) { container.Add(subOption); }
            return container;
        }
        #endregion

        #region Tests
        [Test]
        public void Add_ThenGetSubOptions_ReturnsAllInOrder()
        {
            UIChoiceButton first = CreateWiredButton("First");
            UIChoiceButton second = CreateWiredButton("Second");
            UIChoiceContainer container = CreateContainer(true, first, second);

            IList<UIChoice> subOptions = container.GetSubOptions();

            CollectionAssert.AreEqual(new UIChoice[] { first, second }, subOptions);
        }

        [Test]
        public void TryMove_NoSubOptions_ReturnsFalse()
        {
            UIChoiceContainer container = CreateContainer(true);

            Assert.IsFalse(container.TryMove(ControllerInputType.NavigateRight));
        }

        [Test]
        public void Highlight_True_SelectsFirstSubOptionRegardlessOfPriorState()
        {
            UIChoiceButton first = CreateWiredButton("First");
            UIChoiceButton second = CreateWiredButton("Second");
            UIChoiceContainer container = CreateContainer(true, first, second);

            container.Highlight(true);

            Assert.IsTrue(container.GetSubOptions()[0] == first);
        }

        [Test]
        public void TryMove_HorizontalRight_AdvancesToNextSubOptionWithWraparound()
        {
            UIChoiceButton first = CreateWiredButton("First");
            UIChoiceButton second = CreateWiredButton("Second");
            UIChoiceContainer container = CreateContainer(true, first, second);
            container.Highlight(true); // starts on `first`

            Assert.IsTrue(container.TryMove(ControllerInputType.NavigateRight));
            Assert.IsTrue(container.TryMove(ControllerInputType.NavigateRight)); // wraps back to `first`
        }

        [Test]
        public void TryMove_HorizontalMode_IgnoresVerticalInput()
        {
            UIChoiceButton first = CreateWiredButton("First");
            UIChoiceButton second = CreateWiredButton("Second");
            UIChoiceContainer container = CreateContainer(true, first, second);
            container.Highlight(true);

            Assert.IsFalse(container.TryMove(ControllerInputType.NavigateUp));
            Assert.IsFalse(container.TryMove(ControllerInputType.NavigateDown));
        }

        [Test]
        public void TryMove_VerticalMode_RespondsToUpDownNotLeftRight()
        {
            UIChoiceButton first = CreateWiredButton("First");
            UIChoiceButton second = CreateWiredButton("Second");
            UIChoiceContainer container = CreateContainer(false, first, second);
            container.Highlight(true);

            Assert.IsFalse(container.TryMove(ControllerInputType.NavigateLeft));
            Assert.IsFalse(container.TryMove(ControllerInputType.NavigateRight));
            Assert.IsTrue(container.TryMove(ControllerInputType.NavigateDown));
        }

        [Test]
        public void UseChoice_DelegatesToHighlightedSubOption()
        {
            UIChoiceButton first = CreateWiredButton("First");
            bool clicked = false;
            first.AddOnClickListener(() => clicked = true);
            UIChoiceContainer container = CreateContainer(true, first);
            container.Highlight(true);

            container.UseChoice();

            Assert.IsTrue(clicked);
        }

        [Test]
        public void UseChoice_NoHighlightedSubOption_DoesNotThrow()
        {
            UIChoiceContainer container = CreateContainer(true, CreateWiredButton("First"));

            Assert.DoesNotThrow(container.UseChoice);
        }
        #endregion
    }
}
