using NUnit.Framework;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using LowDefMustard.Utils.Editor;

namespace LowDefMustard.Utils.Tests.Editor
{
    public class ReadOnlyDrawerTests
    {
        private class Host : ScriptableObject
        {
            public int someValue = 5;
        }

        private Host host;

        [SetUp]
        public void SetUp()
        {
            host = ScriptableObject.CreateInstance<Host>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(host);
        }

        [Test]
        public void CreatePropertyGUI_ReturnsPropertyField()
        {
            var property = new SerializedObject(host).FindProperty(nameof(Host.someValue));
            var drawer = new ReadOnlyDrawer();

            var element = drawer.CreatePropertyGUI(property);

            Assert.IsInstanceOf<PropertyField>(element);
        }

        [Test]
        public void CreatePropertyGUI_ReturnsDisabledField()
        {
            var property = new SerializedObject(host).FindProperty(nameof(Host.someValue));
            var drawer = new ReadOnlyDrawer();

            var field = (PropertyField)drawer.CreatePropertyGUI(property);

            Assert.IsFalse(field.enabledSelf);
        }

        [Test]
        public void CreatePropertyGUI_LabelMatchesDisplayName()
        {
            var property = new SerializedObject(host).FindProperty(nameof(Host.someValue));
            var drawer = new ReadOnlyDrawer();

            var field = (PropertyField)drawer.CreatePropertyGUI(property);

            Assert.AreEqual(property.displayName, field.label);
        }

        [Test]
        public void CreatePropertyGUI_BindingPathMatchesPropertyPath()
        {
            var property = new SerializedObject(host).FindProperty(nameof(Host.someValue));
            var drawer = new ReadOnlyDrawer();

            var field = (PropertyField)drawer.CreatePropertyGUI(property);

            Assert.AreEqual(property.propertyPath, field.bindingPath);
        }
    }
}
