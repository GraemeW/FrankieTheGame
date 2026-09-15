using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using LowDefMustard.Control;

namespace LowDefMustard.UIBox.Tests.Editor
{
    public class UIBoxHandleGlobalInputTests
    {
        // State
        private List<GameObject> spawned;

        // Setup
        [SetUp]
        public void SetUp() => spawned = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in spawned.Where(go => go != null))
            {
                Object.DestroyImmediate(go);
            }
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
        
        #region HandleGlobalInput
        [Test]
        public void HandleGlobalInput_Disabled_ReturnsTrueWithoutActingOnInput()
        {
            TestUIBox testUIBox = CreateTestUIBox();
            UIChoiceButton choice = CreateWiredButton("Choice");
            testUIBox.InjectChoiceOptions(new UIChoice[] { choice });
            testUIBox.PublicSetChoiceAvailable(true);
            testUIBox.publicHandleGlobalInput = false;

            bool result = testUIBox.PublicStandardHandleGlobalInput(ControllerInputType.NavigateRight);

            Assert.IsTrue(result);
            Assert.IsNull(testUIBox.publicHighlightedChoiceOption); // input was never actually acted on
        }

        [Test]
        public void HandleGlobalInput_CancelWithNoChoicesAvailable_FallsThroughToEarlyExit()
        {
            TestUIBox testUIBox = CreateTestUIBox();
            testUIBox.publicHandleGlobalInput = true;

            testUIBox.GetInputHandler().Invoke(ControllerInputType.Cancel);

            Assert.IsTrue(testUIBox.destroyQueued);
        }

        [Test]
        public void HandleGlobalInput_NonExitInputWithNoChoicesAvailable_ReturnsFalse()
        {
            TestUIBox testUIBox = CreateTestUIBox();
            testUIBox.publicHandleGlobalInput = true;
            testUIBox.PublicSetChoiceAvailable(false);

            bool result = testUIBox.PublicStandardHandleGlobalInput(ControllerInputType.NavigateRight);

            Assert.IsFalse(result);
        }

        [Test]
        public void HandleGlobalInput_FirstInteractionWithAvailableChoices_ShowsCursorOnFirstOption()
        {
            TestUIBox testUIBox = CreateTestUIBox();
            UIChoiceButton first = CreateWiredButton("First");
            UIChoiceButton second = CreateWiredButton("Second");
            testUIBox.InjectChoiceOptions(new UIChoice[] { first, second });
            testUIBox.PublicSetChoiceAvailable(true);
            testUIBox.publicHandleGlobalInput = true;

            bool result = testUIBox.PublicStandardHandleGlobalInput(ControllerInputType.NavigateRight);

            Assert.IsTrue(result);
            Assert.AreSame(first, testUIBox.publicHighlightedChoiceOption);
        }

        [Test]
        public void HandleGlobalInput_AlreadyHighlighted_MovesCursorInstead()
        {
            TestUIBox testUIBox = CreateTestUIBox();
            UIChoiceButton first = CreateWiredButton("First");
            UIChoiceButton second = CreateWiredButton("Second");
            testUIBox.InjectChoiceOptions(new UIChoice[] { first, second });
            testUIBox.SetHighlightedChoiceOption(first);
            testUIBox.PublicSetChoiceAvailable(true);
            testUIBox.publicHandleGlobalInput = true;

            bool result = testUIBox.PublicStandardHandleGlobalInput(ControllerInputType.NavigateRight);

            Assert.IsTrue(result);
            Assert.AreSame(second, testUIBox.publicHighlightedChoiceOption);
        }

        [Test]
        public void HandleGlobalInput_ExecuteOnHighlightedChoice_ChoosesIt()
        {
            TestUIBox testUIBox = CreateTestUIBox();
            UIChoiceButton choice = CreateWiredButton("Choice");
            bool clicked = false;
            choice.AddOnClickListener(() => clicked = true);
            testUIBox.InjectChoiceOptions(new UIChoice[] { choice });
            testUIBox.SetHighlightedChoiceOption(choice);
            testUIBox.PublicSetChoiceAvailable(true);
            testUIBox.publicHandleGlobalInput = true;

            bool result = testUIBox.PublicStandardHandleGlobalInput(ControllerInputType.Execute);

            Assert.IsTrue(result);
            Assert.IsTrue(clicked);
        }
        #endregion

        #region ShowCursorOnAnyInteraction
        [TestCase(ControllerInputType.DefaultNone)]
        [TestCase(ControllerInputType.Cancel)]
        [TestCase(ControllerInputType.Option)]
        public void ShowCursorOnAnyInteraction_ExcludedInputTypes_NeverConsumesTheInput(ControllerInputType input)
        {
            TestUIBox testUIBox = CreateTestUIBox();
            UIChoiceButton choice = CreateWiredButton("Choice");
            testUIBox.InjectChoiceOptions(new UIChoice[] { choice });
            testUIBox.PublicSetChoiceAvailable(true);

            Assert.IsFalse(testUIBox.PublicShowCursorOnAnyInteraction(input));
            Assert.IsNull(testUIBox.publicHighlightedChoiceOption);
        }

        [Test]
        public void ShowCursorOnAnyInteraction_AlreadyHighlighted_ReturnsFalse()
        {
            TestUIBox testUIBox = CreateTestUIBox();
            UIChoiceButton choice = CreateWiredButton("Choice");
            testUIBox.InjectChoiceOptions(new UIChoice[] { choice });
            testUIBox.SetHighlightedChoiceOption(choice);
            testUIBox.PublicSetChoiceAvailable(true);

            Assert.IsFalse(testUIBox.PublicShowCursorOnAnyInteraction(ControllerInputType.NavigateRight));
        }
        #endregion

        #region SetActiveInput
        [Test]
        public void SetActiveInput_False_ReconcilesOptionsAndDisablesGlobalInput()
        {
            TestUIBox testUIBox = CreateTestUIBox();
            UIChoiceButton choice = CreateWiredButton("Choice");
            testUIBox.InjectChoiceOptions(new UIChoice[] { choice, null });
            testUIBox.publicHandleGlobalInput = true;

            testUIBox.SetActiveInput(false);

            CollectionAssert.DoesNotContain(testUIBox.publicChoiceOptions, null);
            Assert.IsTrue(testUIBox.PublicIsChoiceAvailable());
            Assert.IsFalse(testUIBox.publicHandleGlobalInput);
        }
        #endregion
    }
}
