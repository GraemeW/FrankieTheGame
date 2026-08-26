using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using LowDefMustard.Utils.Editor;

namespace LowDefMustard.Utils.Tests.Editor
{
    // Covers:
    // - pure helper logic directly via internal method calls
    // - static construction of VisualElement
    // NOT covered (would require attached UI Toolkit panel or prefab instance, out of scope):
    // - live event-driven write-back (RegisterValueChangedCallback/TrackPropertyValue)
    // - prefab-override visuals/context menu
    
    public class RestrictedEnumDrawerTests
    {
        private enum SampleEnum
        {
            Alpha = 0,
            Beta = 1,
            Gamma = 2,
            Delta = 3
        }

        private enum NicifyEnum
        {
            FirstValue,
            SecondValue
        }

        private class Host : ScriptableObject
        {
            public SampleEnum plainEnum;
            public int notAnEnum;
            public List<SampleEnum> enumList = new() { SampleEnum.Beta };
        }

        private Host host;
        private FieldInfo plainEnumFieldInfo;
        private FieldInfo enumListFieldInfo;

        [SetUp]
        public void SetUp()
        {
            host = ScriptableObject.CreateInstance<Host>();
            plainEnumFieldInfo = typeof(Host).GetField(nameof(Host.plainEnum), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            enumListFieldInfo = typeof(Host).GetField(nameof(Host.enumList), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(host);
        }

        [Test]
        public void RestrictedEnumAttribute_Constructor_StoresHiddenValues()
        {
            var attribute = new RestrictedEnumAttribute(1, 3);

            CollectionAssert.AreEqual(new[] { 1, 3 }, attribute.hiddenValues);
        }

        [Test]
        public void GenerateAllowedNames_ExcludesHiddenValues()
        {
            var hidden = new HashSet<int> { 1, 3 };
            string[] allNames = { "Alpha", "Beta", "Gamma", "Delta" };
            int[] allValues = { 0, 1, 2, 3 };

            RestrictedEnumDrawer.GenerateAllowedNames(hidden, allNames, allValues, out var allowedNames, out var allowedValues);

            CollectionAssert.AreEqual(new[] { "Alpha", "Gamma" }, allowedNames);
            CollectionAssert.AreEqual(new[] { 0, 2 }, allowedValues);
        }

        [Test]
        public void GenerateAllowedNames_NicifiesEnumMemberNames()
        {
            var hidden = new HashSet<int>();
            string[] allNames = System.Enum.GetNames(typeof(NicifyEnum));
            int[] allValues = { 0, 1 };

            RestrictedEnumDrawer.GenerateAllowedNames(hidden, allNames, allValues, out var allowedNames, out _);

            CollectionAssert.AreEqual(new[] { "First Value", "Second Value" }, allowedNames);
        }

        [Test]
        public void GetEnumType_PlainEnumField_ResolvesToDeclaredEnum()
        {
            var resolved = RestrictedEnumDrawer.GetEnumType(plainEnumFieldInfo);

            Assert.AreEqual(typeof(SampleEnum), resolved);
        }

        [Test]
        public void GetEnumType_ListFieldInfo_ResolvesElementType()
        {
            // Exercises the List<> unwrapping logic
            var resolved = RestrictedEnumDrawer.GetEnumType(enumListFieldInfo);

            Assert.AreEqual(typeof(SampleEnum), resolved);
        }

        [Test]
        public void GetEnumType_NonEnumField_ReturnsNull()
        {
            var fieldInfo = typeof(Host).GetField(nameof(Host.notAnEnum), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            var resolved = RestrictedEnumDrawer.GetEnumType(fieldInfo);

            Assert.IsNull(resolved);
        }

        [Test]
        public void GetEnumValues_PlainEnumField_ReturnsUnderlyingIntValues()
        {
            var property = new SerializedObject(host).FindProperty(nameof(Host.plainEnum));

            var values = RestrictedEnumDrawer.GetEnumValues(property, plainEnumFieldInfo);

            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3 }, values);
        }

        [Test]
        public void GetEnumValues_NoResolvableFieldInfo_FallsBackToSequentialIndices()
        {
            // When fieldInfo can't resolve an enum Type (e.g. null), GetEnumValues falls back to treating enumValueIndex itself as the value: 0...N-1.
            var property = new SerializedObject(host).FindProperty(nameof(Host.plainEnum));

            var values = RestrictedEnumDrawer.GetEnumValues(property, fieldInfo: null);

            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3 }, values);
        }

        [Test]
        public void SetSerializedValue_SetsUnderlyingIntValue()
        {
            var property = new SerializedObject(host).FindProperty(nameof(Host.plainEnum));

            RestrictedEnumDrawer.SetSerializedValue(property, newIntValue: 2);

            Assert.AreEqual(2, property.intValue);
            Assert.AreEqual(2, property.enumValueIndex); // Gamma; values line up with indices in SampleEnum
        }

        [Test]
        public void IsValidInput_EnumProperty_ReturnsTrueWithRestriction()
        {
            var property = new SerializedObject(host).FindProperty(nameof(Host.plainEnum));
            var attribute = new RestrictedEnumAttribute(1);

            bool result = RestrictedEnumDrawer.IsValidInput(property, attribute, out var restriction);

            Assert.IsTrue(result);
            Assert.AreSame(attribute, restriction);
        }

        [Test]
        public void IsValidInput_NonEnumProperty_ReturnsFalse()
        {
            var property = new SerializedObject(host).FindProperty(nameof(Host.notAnEnum));
            var attribute = new RestrictedEnumAttribute(1);

            bool result = RestrictedEnumDrawer.IsValidInput(property, attribute, out var restriction);

            Assert.IsFalse(result);
            Assert.IsNull(restriction);
        }

        [Test]
        public void CreatePropertyGUI_NonEnumProperty_ReturnsHelpBoxError()
        {
            var property = new SerializedObject(host).FindProperty(nameof(Host.notAnEnum));
            var notAnEnumFieldInfo = typeof(Host).GetField(nameof(Host.notAnEnum), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var drawer = CreateDrawer(new RestrictedEnumAttribute(), notAnEnumFieldInfo);

            var element = drawer.CreatePropertyGUI(property);

            Assert.IsInstanceOf<HelpBox>(element);
            StringAssert.Contains("requires an enum field", ((HelpBox)element).text);
        }

        [Test]
        public void CreatePropertyGUI_ValidEnumProperty_ReturnsDropdownWithAllowedChoices()
        {
            host.plainEnum = SampleEnum.Gamma;
            var property = new SerializedObject(host).FindProperty(nameof(Host.plainEnum));
            var drawer = CreateDrawer(new RestrictedEnumAttribute(1), plainEnumFieldInfo); // hide Beta

            var dropdown = drawer.CreatePropertyGUI(property) as DropdownField;

            Assert.IsNotNull(dropdown, "Expected a DropdownField for a valid enum property");
            CollectionAssert.AreEqual(new[] { "Alpha", "Gamma", "Delta" }, dropdown.choices);
            Assert.AreEqual("Gamma", dropdown.value);
        }

        [Test]
        public void CreatePropertyGUI_CurrentValueIsHidden_FallsBackToFirstAllowedChoice()
        {
            host.plainEnum = SampleEnum.Beta;
            var property = new SerializedObject(host).FindProperty(nameof(Host.plainEnum));
            var drawer = CreateDrawer(new RestrictedEnumAttribute(1), plainEnumFieldInfo); // hides the current value

            var dropdown = (DropdownField)drawer.CreatePropertyGUI(property);

            Assert.AreEqual("Alpha", dropdown.value); // first allowed choice
        }

        [Test]
        public void ResolvePopupIndex_CurrentValueAllowed_ReturnsMatchingIndex()
        {
            var allowedValues = new List<int> { 0, 2, 3 }; // Beta (1) hidden
            host.plainEnum = SampleEnum.Gamma; // value 2
            var property = new SerializedObject(host).FindProperty(nameof(Host.plainEnum));

            int index = RestrictedEnumDrawer.ResolvePopupIndex(property, allowedValues);

            Assert.AreEqual(1, index); // Gamma is at index 1 within allowedValues
        }

        [Test]
        public void ResolvePopupIndex_CurrentValueHidden_ReturnsZero()
        {
            var allowedValues = new List<int> { 0, 2, 3 }; // Beta (1) hidden
            host.plainEnum = SampleEnum.Beta; // the hidden value
            var property = new SerializedObject(host).FindProperty(nameof(Host.plainEnum));

            int index = RestrictedEnumDrawer.ResolvePopupIndex(property, allowedValues);

            Assert.AreEqual(0, index);
        }

        // PropertyDrawer.attribute and .fieldInfo have no public setters
        // Unity's own machinery populates both when a drawer is used for real "m_Attribute"/"m_FieldInfo" the backing field names
        private static RestrictedEnumDrawer CreateDrawer(RestrictedEnumAttribute attribute, FieldInfo fieldInfo)
        {
            var drawer = new RestrictedEnumDrawer();
            SetBackingField(drawer, "m_Attribute", attribute);
            SetBackingField(drawer, "m_FieldInfo", fieldInfo);
            return drawer;
        }

        private static void SetBackingField(PropertyDrawer drawer, string backingFieldName, object value)
        {
            var backingField = typeof(PropertyDrawer).GetField(backingFieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            backingField?.SetValue(drawer, value);
        }
    }
}
