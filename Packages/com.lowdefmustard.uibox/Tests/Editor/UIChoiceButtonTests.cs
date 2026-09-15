using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace LowDefMustard.UIBox.Tests.Editor
{
    public class UIChoiceButtonTests
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
        private UIChoiceButton CreateWiredButton(string name) => TestChoiceFactory.CreateWiredButton(name, spawned);

        // Tests
        [Test]
        public void UseChoice_InvokesButtonOnClick()
        {
            UIChoiceButton choice = CreateWiredButton("Choice");
            bool clicked = false;
            choice.AddOnClickListener(() => clicked = true);

            choice.UseChoice();

            Assert.IsTrue(clicked);
        }

        [Test]
        public void RemoveOnClickListeners_StopsFurtherInvocations()
        {
            UIChoiceButton choice = CreateWiredButton("Choice");
            bool clicked = false;
            choice.AddOnClickListener(() => clicked = true);

            choice.RemoveOnClickListeners();
            choice.UseChoice();

            Assert.IsFalse(clicked);
        }

        [Test]
        public void AddOnClickListener_NullAction_IsIgnored()
        {
            UIChoiceButton choice = CreateWiredButton("Choice");

            Assert.DoesNotThrow(() => choice.AddOnClickListener(null));
        }
    }
}
