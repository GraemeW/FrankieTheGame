using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using LowDefMustard.Utils.Editor;
using Object = UnityEngine.Object;

namespace LowDefMustard.Utils.Tests.Editor
{
    public class SerializedPropertyExtensionsTests
    {
        // Tested:
        //  - IsStringArray - List<string> and string[] (true);  a string field, a list of classes (false)
        //  - FindParentProperty - nested field, list element (the list's .Array node, usable as the list), top level (null)
        //  - FindSiblingProperty - nested sibling, top-level sibling (looked up on the object), missing sibling (null)
        //  - TryFindOwningArray - simple, nested (innermost list wins), field inside a struct, not in a list, the element itself
        // Not Tested: Managed references

        // State
        private Host host;
        private SerializedObject serializedObject;

        #region DataStructures
        [Serializable] private class Inner { public int a; public int b; }
        [Serializable] private class Outer { public List<Inner> inners = new(); }
        [Serializable] private class Wrapped { public Inner inner = new(); }

        private class Host : ScriptableObject
        {
            public Inner inner = new();
            public List<Inner> inners = new();
            public List<Outer> outers = new();
            public List<Wrapped> wrappeds = new();
            public int topA;
            public int topB;
            public string topName;
            public List<string> names = new();
            public string[] nameArray = Array.Empty<string>();
        }
        #endregion
        
        #region Setup
        [SetUp]
        public void SetUp()
        {
            host = ScriptableObject.CreateInstance<Host>();
            host.inners.Add(new Inner());
            host.inners.Add(new Inner());
            host.inners.Add(new Inner());
            host.outers.Add(new Outer());
            host.outers.Add(new Outer());
            for (int i = 0; i < 4; i++) { host.outers[1].inners.Add(new Inner()); }
            host.wrappeds.Add(new Wrapped());
            serializedObject = new SerializedObject(host);
        }

        [TearDown]
        public void TearDown()
        {
            serializedObject?.Dispose();
            Object.DestroyImmediate(host);
        }
        #endregion

        #region Tests
        [TestCase(nameof(Host.names), true)]
        [TestCase(nameof(Host.nameArray), true)]
        [TestCase(nameof(Host.topName), false)]
        [TestCase(nameof(Host.inners), false)]
        public void IsStringArray_Field_MatchesStringCollections(string path, bool expected)
        {
            Assert.AreEqual(expected, serializedObject.FindProperty(path).IsStringArray());
        }

        [Test]
        public void FindParentProperty_NestedField_ReturnsOwner()
        {
            SerializedProperty parent = serializedObject.FindProperty($"{nameof(Host.inner)}.{nameof(Inner.a)}").FindParentProperty();

            Assert.AreEqual(nameof(Host.inner), parent.propertyPath);
        }

        [Test]
        public void FindParentProperty_ListElement_ReturnsArrayNodeUsableAsList()
        {
            SerializedProperty element = serializedObject.FindProperty(nameof(Host.inners)).GetArrayElementAtIndex(1);

            SerializedProperty parent = element.FindParentProperty();

            Assert.IsTrue(parent.isArray);
            Assert.AreEqual(3, parent.arraySize);
            Assert.IsTrue(SerializedProperty.EqualContents(element, parent.GetArrayElementAtIndex(1)));
        }

        [Test]
        public void FindParentProperty_TopLevel_Null()
        {
            Assert.IsNull(serializedObject.FindProperty(nameof(Host.topA)).FindParentProperty());
        }

        [Test]
        public void FindSiblingProperty_NestedField_FindsSibling()
        {
            SerializedProperty sibling = serializedObject.FindProperty(nameof(Host.inners)).GetArrayElementAtIndex(1).FindPropertyRelative(nameof(Inner.a)).FindSiblingProperty(nameof(Inner.b));

            Assert.IsTrue(SerializedProperty.EqualContents(serializedObject.FindProperty(nameof(Host.inners)).GetArrayElementAtIndex(1).FindPropertyRelative(nameof(Inner.b)), sibling));
        }

        [Test]
        public void FindSiblingProperty_TopLevel_FindsOnObject()
        {
            SerializedProperty sibling = serializedObject.FindProperty(nameof(Host.topA)).FindSiblingProperty(nameof(Host.topB));

            Assert.AreEqual(nameof(Host.topB), sibling.propertyPath);
        }

        [Test]
        public void FindSiblingProperty_Missing_Null()
        {
            Assert.IsNull(serializedObject.FindProperty($"{nameof(Host.inner)}.{nameof(Inner.a)}").FindSiblingProperty("missing"));
        }

        [TestCase("inners.Array.data[2].a", "inners.Array", "a", 2)]
        [TestCase("outers.Array.data[1].inners.Array.data[3].b", "outers.Array.data[1].inners.Array", "b", 3)]
        [TestCase("wrappeds.Array.data[0].inner.a", "wrappeds.Array", "inner.a", 0)]
        public void TryFindOwningArray_ElementField_FindsInnermostList(string path, string expectedArrayPath, string expectedRelativePath, int expectedIndex)
        {
            Assert.IsTrue(serializedObject.FindProperty(path).TryFindOwningArray(out SerializedProperty arrayProperty, out int index, out string relativePath));
            Assert.AreEqual(expectedArrayPath, arrayProperty.propertyPath);
            Assert.AreEqual(expectedRelativePath, relativePath);
            Assert.AreEqual(expectedIndex, index);
        }

        [TestCase("topA")]
        [TestCase("inner.a")]
        [TestCase("inners.Array.data[2]")]
        public void TryFindOwningArray_NotAnElementField_ReturnsFalse(string path)
        {
            Assert.IsFalse(serializedObject.FindProperty(path).TryFindOwningArray(out _, out _, out _));
        }
        #endregion
    }
}
