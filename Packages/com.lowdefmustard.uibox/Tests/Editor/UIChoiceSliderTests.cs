using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using LowDefMustard.Control;

namespace LowDefMustard.UIBox.Tests.Editor
{
    // Test Notes:
    //  - sliderAdjustmentStep defaults to 0.1f in production code
    //  - tests here rely on that default directly rather than overriding it per test
    
    public class UIChoiceSliderTests
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

        // Private Methods
        private UIChoiceSlider CreateWiredSlider(string name, float min = 0f, float max = 1f) => TestChoiceFactory.CreateWiredSlider(name, spawned, min, max);

        #region Tests
        [Test]
        public void TryMove_Right_IncreasesValueByStep()
        {
            UIChoiceSlider choice = CreateWiredSlider("Choice");
            choice.SetSliderValue(0.5f);

            bool handled = ((IUIMoveInterceptor)choice).TryMove(ControllerInputType.NavigateRight);

            Assert.IsTrue(handled);
            Assert.AreEqual(0.6f, choice.GetSliderValue(), 0.0001f);
        }

        [Test]
        public void TryMove_Left_DecreasesValueByStep()
        {
            UIChoiceSlider choice = CreateWiredSlider("Choice");
            choice.SetSliderValue(0.5f);

            bool handled = ((IUIMoveInterceptor)choice).TryMove(ControllerInputType.NavigateLeft);

            Assert.IsTrue(handled);
            Assert.AreEqual(0.4f, choice.GetSliderValue(), 0.0001f);
        }

        [Test]
        public void TryMove_Right_ClampsAtMax()
        {
            UIChoiceSlider choice = CreateWiredSlider("Choice");
            choice.SetSliderValue(0.95f);

            ((IUIMoveInterceptor)choice).TryMove(ControllerInputType.NavigateRight);

            Assert.AreEqual(1f, choice.GetSliderValue(), 0.0001f);
        }

        [Test]
        public void TryMove_Left_ClampsAtMin()
        {
            UIChoiceSlider choice = CreateWiredSlider("Choice");
            choice.SetSliderValue(0.05f);

            ((IUIMoveInterceptor)choice).TryMove(ControllerInputType.NavigateLeft);

            Assert.AreEqual(0f, choice.GetSliderValue(), 0.0001f);
        }

        [Test]
        public void TryMove_UnhandledInput_ReturnsFalseAndLeavesValueUnchanged()
        {
            UIChoiceSlider choice = CreateWiredSlider("Choice");
            choice.SetSliderValue(0.5f);

            bool handled = ((IUIMoveInterceptor)choice).TryMove(ControllerInputType.NavigateUp);

            Assert.IsFalse(handled);
            Assert.AreEqual(0.5f, choice.GetSliderValue(), 0.0001f);
        }

        [Test]
        public void UseChoice_DoesNotThrow()
        {
            UIChoiceSlider choice = CreateWiredSlider("Choice");

            Assert.DoesNotThrow(choice.UseChoice);
        }

        [Test]
        public void SetSliderValue_FiresValueChangedListener()
        {
            UIChoiceSlider choice = CreateWiredSlider("Choice");
            float? received = null;
            choice.AddOnValueChangeListener(value => received = value);

            choice.SetSliderValue(0.75f);

            Assert.AreEqual(0.75f, received, 0.0001f);
        }

        [Test]
        public void AddOnValueChangeListener_NullAction_IsIgnored()
        {
            UIChoiceSlider choice = CreateWiredSlider("Choice");

            Assert.DoesNotThrow(() => choice.AddOnValueChangeListener(null));
        }
        #endregion
    }
}
