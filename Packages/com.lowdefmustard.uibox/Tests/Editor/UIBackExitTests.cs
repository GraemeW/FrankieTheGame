using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LowDefMustard.UIBox.Tests.Editor
{
    public class UIBackExitTests
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
        private UIBackExit CreateBackExit(UIChoiceButton wiredButton)
        {
            var go = new GameObject("BackExit");
            spawned.Add(go);
            var backExit = go.AddComponent<UIBackExit>();

            var serializedObject = new SerializedObject(backExit);
            serializedObject.FindProperty("backExitButton").objectReferenceValue = wiredButton;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            return backExit;
        }

        // Tests
        [Test]
        public void SetBackExitClickBehaviour_NoBackExitButtonAssigned_DoesNotThrow()
        {
            UIBackExit backExit = CreateBackExit(null);

            Assert.DoesNotThrow(() => backExit.SetBackExitClickBehaviour(() => { }));
        }

        [Test]
        public void SetBackExitClickBehaviour_WiredButton_ClickInvokesSuppliedAction()
        {
            UIChoiceButton button = TestChoiceFactory.CreateWiredButton("BackExitButton", spawned);
            UIBackExit backExit = CreateBackExit(button);
            bool invoked = false;

            backExit.SetBackExitClickBehaviour(() => invoked = true);
            button.UseChoice();

            Assert.IsTrue(invoked);
        }
    }
}
