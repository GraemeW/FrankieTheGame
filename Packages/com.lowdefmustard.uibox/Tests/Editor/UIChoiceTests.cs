using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace LowDefMustard.UIBox.Tests.Editor
{
    // Test Notes:
    //  - OnEnable/OnDisable's extra-listener resubscription lives in Tests/Runtime
    //  - Not covered: DisableHighlightListeners' persistent-listener state (thin wrapper over Unity's API, skipped)

    public class UIChoiceTests
    {
        // State
        private readonly List<GameObject> spawned = new();

        // Setup
        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in spawned)
            {
                if (go != null) { Object.DestroyImmediate(go); }
            }
            spawned.Clear();
        }

        #region PrivateMethods
        private UIChoiceButton CreateWiredButton(string name) => TestChoiceFactory.CreateWiredButton(name, spawned);

        private static void SetField(Object target, string fieldName, object value)
        {
            var serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(fieldName);
            switch (value)
            {
                case Object objectValue: property.objectReferenceValue = objectValue; break;
                case bool boolValue: property.boolValue = boolValue; break;
                case Color colorValue: property.colorValue = colorValue; break;
            }
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
        #endregion

        #region Tests
        [Test]
        public void Highlight_True_ActivatesSelectionMarker()
        {
            UIChoiceButton choice = CreateWiredButton("Choice");
            var marker = new GameObject("Marker");
            spawned.Add(marker);
            marker.SetActive(false);
            SetField(choice, "selectionMarker", marker);

            choice.Highlight(true);

            Assert.IsTrue(marker.activeSelf);
        }

        [Test]
        public void Highlight_False_DeactivatesSelectionMarker()
        {
            UIChoiceButton choice = CreateWiredButton("Choice");
            var marker = new GameObject("Marker");
            spawned.Add(marker);
            SetField(choice, "selectionMarker", marker);
            choice.Highlight(true);

            choice.Highlight(false);

            Assert.IsFalse(marker.activeSelf);
        }

        [Test]
        public void Highlight_True_InvokesItemHighlightedEvent()
        {
            UIChoiceButton choice = CreateWiredButton("Choice");
            bool invoked = false;
            choice.itemHighlighted.AddListener(() => invoked = true);

            choice.Highlight(true);

            Assert.IsTrue(invoked);
        }

        [Test]
        public void Highlight_False_DoesNotInvokeItemHighlightedEvent()
        {
            UIChoiceButton choice = CreateWiredButton("Choice");
            bool invoked = false;
            choice.itemHighlighted.AddListener(() => invoked = true);

            choice.Highlight(false);

            Assert.IsFalse(invoked);
        }

        [Test]
        public void UseInvalidChoiceDimming_Enabled_SetsColorByHighlightState()
        {
            UIChoiceButton choice = CreateWiredButton("Choice");
            var textField = choice.gameObject.AddComponent<TextMeshProUGUI>();
            SetField(choice, "textField", textField);
            SetField(choice, "validChoiceColor", Color.white);
            SetField(choice, "invalidChoiceColor", Color.gray);
            choice.UseInvalidChoiceDimming(true);

            choice.Highlight(false);
            Assert.AreEqual(Color.gray, textField.color);

            choice.Highlight(true);
            Assert.AreEqual(Color.white, textField.color);
        }

        [Test]
        public void UseHighlightSelected_Enabled_SetsColorAndBoldByHighlightState()
        {
            UIChoiceButton choice = CreateWiredButton("Choice");
            var textField = choice.gameObject.AddComponent<TextMeshProUGUI>();
            SetField(choice, "textField", textField);
            SetField(choice, "selectHighlightColor", Color.lawnGreen);
            SetField(choice, "validChoiceColor", Color.white);
            choice.UseHighlightSelected(true);

            choice.Highlight(true);
            Assert.AreEqual(Color.lawnGreen, textField.color);
            Assert.AreEqual(FontStyles.Bold, textField.fontStyle);

            choice.Highlight(false);
            Assert.AreEqual(Color.white, textField.color);
            Assert.AreEqual(FontStyles.Normal, textField.fontStyle);
        }

        [Test]
        public void SetChoiceOrder_UpdatesPublicChoiceOrderField()
        {
            UIChoiceButton choice = CreateWiredButton("Choice");

            choice.SetChoiceOrder(7);

            Assert.AreEqual(7, choice.choiceOrder);
        }

        [Test]
        public void AddOnHighlightListener_WhileActive_AttachesImmediately()
        {
            UIChoiceButton choice = CreateWiredButton("Choice");
            bool invoked = false;

            choice.AddOnHighlightListener(() => invoked = true);
            choice.Highlight(true);

            Assert.IsTrue(invoked);
        }

        [Test]
        public void AddOnHighlightListener_NullAction_IsIgnored()
        {
            UIChoiceButton choice = CreateWiredButton("Choice");

            Assert.DoesNotThrow(() => choice.AddOnHighlightListener(null));
        }
        #endregion
    }
}
