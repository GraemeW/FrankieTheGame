using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace LowDefMustard.UIBox.Tests.Editor
{
    public class UIChoiceToggleTests
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
        private UIChoiceToggle CreateWiredToggle(string name) => TestChoiceFactory.CreateWiredToggle(name, spawned);

        #region Tests
        [Test]
        public void UseChoice_FlipsToggleValue()
        {
            UIChoiceToggle choice = CreateWiredToggle("Choice");
            choice.SetToggleValueSilently(false);

            choice.UseChoice();

            Assert.IsTrue(choice.GetToggleValue());
        }

        [Test]
        public void UseChoice_CalledTwice_TogglesBackToOriginal()
        {
            UIChoiceToggle choice = CreateWiredToggle("Choice");
            choice.SetToggleValueSilently(false);

            choice.UseChoice();
            choice.UseChoice();

            Assert.IsFalse(choice.GetToggleValue());
        }

        [Test]
        public void SetToggleValueSilently_DoesNotFireValueChangedListener()
        {
            UIChoiceToggle choice = CreateWiredToggle("Choice");
            bool fired = false;
            choice.AddOnValueChangeListener(_ => fired = true);

            choice.SetToggleValueSilently(true);

            Assert.IsFalse(fired);
            Assert.IsTrue(choice.GetToggleValue());
        }

        [Test]
        public void SetToggleValue_FiresValueChangedListener()
        {
            UIChoiceToggle choice = CreateWiredToggle("Choice");
            choice.SetToggleValueSilently(false);
            bool fired = false;
            choice.AddOnValueChangeListener(_ => fired = true);

            choice.SetToggleValue(true);

            Assert.IsTrue(fired);
        }

        [Test]
        public void AddOnValueChangeListener_NullAction_IsIgnored()
        {
            UIChoiceToggle choice = CreateWiredToggle("Choice");

            Assert.DoesNotThrow(() => choice.AddOnValueChangeListener(null));
        }
        #endregion
    }
}
