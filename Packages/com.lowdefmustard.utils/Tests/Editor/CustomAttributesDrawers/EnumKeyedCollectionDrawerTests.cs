using System;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;
using LowDefMustard.Utils.Editor;

namespace LowDefMustard.Utils.Tests.Editor
{
    public class EnumKeyedCollectionDrawerTests
    {
        private enum SampleEnum
        {
            Alpha,
            Beta,
            Gamma
        }

        // Real, valid target - a proper EnumKeyedCollection<TEnum,TData> field
        private class ValidHost : ScriptableObject
        {
            [EnumKeyedCollection] public EnumKeyedCollection<SampleEnum, int> values = new();
        }

        // Attribute applied to a field whose type doesn't implement IEnumKeyedCollection
        private class WrongTypeHost : ScriptableObject
        {
            [EnumKeyedCollection] public int notACollection;
        }

        // Minimal IEnumKeyedCollection that reports a null enum type
        [Serializable]
        private class NullEnumTypeStub : IEnumKeyedCollection
        {
            [SerializeField] private int dummy; // ensures Unity can build a type-tree node for boxedValue
            public void SyncEntriesToEnum() { }
            public Type GetEnumType() => null;
            public string GetListName() => "entries";
        }

        private class NullEnumTypeHost : ScriptableObject
        {
            [EnumKeyedCollection] public NullEnumTypeStub stub = new();
        }

        // Minimal IEnumKeyedCollection that points at a field name that doesn't actually exist on it, to hit the "could not find serialized list" branch
        [Serializable]
        private class WrongListNameStub : IEnumKeyedCollection
        {
            [SerializeField] private int dummy; // ensures Unity can build a type-tree node for boxedValue
            public void SyncEntriesToEnum() { }
            public Type GetEnumType() => typeof(SampleEnum);
            public string GetListName() => "thisFieldDoesNotExist";
        }

        private class WrongListNameHost : ScriptableObject
        {
            [EnumKeyedCollection] public WrongListNameStub stub = new();
        }

        private ScriptableObject createdInstance;

        [TearDown]
        public void TearDown()
        {
            if (createdInstance != null) { Object.DestroyImmediate(createdInstance); }
        }

        [Test]
        public void CreatePropertyGUI_ValidField_ReturnsFoldoutWithOneFieldPerEnumValue()
        {
            var host = CreateHost<ValidHost>();
            var property = new SerializedObject(host).FindProperty(nameof(ValidHost.values));
            var drawer = new EnumKeyedCollectionDrawer();

            var root = drawer.CreatePropertyGUI(property);

            var foldout = root.Q<Foldout>();
            Assert.IsNotNull(foldout, "Expected a Foldout for a valid field");
            // SyncEntriesToEnum should have populated one entry per SampleEnum value
            Assert.AreEqual(3, foldout.Query<PropertyField>().ToList().Count);
        }

        [Test]
        public void CreatePropertyGUI_ValidField_FoldoutLabelMatchesDisplayName()
        {
            var host = CreateHost<ValidHost>();
            var property = new SerializedObject(host).FindProperty(nameof(ValidHost.values));
            var drawer = new EnumKeyedCollectionDrawer();

            var root = drawer.CreatePropertyGUI(property);

            Assert.AreEqual(property.displayName, root.Q<Foldout>().text);
        }

        [Test]
        public void CreatePropertyGUI_SyncedEntries_ArePersistedToSerializedObject()
        {
            var host = CreateHost<ValidHost>();
            var serializedObject = new SerializedObject(host);
            var property = serializedObject.FindProperty(nameof(ValidHost.values));
            var drawer = new EnumKeyedCollectionDrawer();

            drawer.CreatePropertyGUI(property);

            // Re-fetch from a fresh SerializedObject to confirm ApplyModifiedProperties actually landed // not just an in-memory boxedValue mutation
            var reread = new SerializedObject(host).FindProperty(nameof(ValidHost.values));
            var entries = reread.FindPropertyRelative("entries");
            Assert.AreEqual(3, entries.arraySize);
        }

        [Test]
        public void CreatePropertyGUI_TargetNotIEnumKeyedCollection_ReturnsErrorHelpBox()
        {
            var host = CreateHost<WrongTypeHost>();
            var property = new SerializedObject(host).FindProperty(nameof(WrongTypeHost.notACollection));
            var drawer = new EnumKeyedCollectionDrawer();

            var root = drawer.CreatePropertyGUI(property);

            var helpBox = root.Q<HelpBox>();
            Assert.IsNotNull(helpBox);
            StringAssert.Contains("must implement IEnumKeyedCollection", helpBox.text);
        }

        [Test]
        public void CreatePropertyGUI_NullEnumType_ReturnsErrorHelpBox()
        {
            var host = CreateHost<NullEnumTypeHost>();
            var property = new SerializedObject(host).FindProperty(nameof(NullEnumTypeHost.stub));
            var drawer = new EnumKeyedCollectionDrawer();

            var root = drawer.CreatePropertyGUI(property);

            var helpBox = root.Q<HelpBox>();
            Assert.IsNotNull(helpBox);
            StringAssert.Contains("invalid or null Enum type", helpBox.text);
        }

        [Test]
        public void CreatePropertyGUI_ListNameDoesNotMatchAnyField_ReturnsErrorHelpBox()
        {
            var host = CreateHost<WrongListNameHost>();
            var property = new SerializedObject(host).FindProperty(nameof(WrongListNameHost.stub));
            var drawer = new EnumKeyedCollectionDrawer();

            var root = drawer.CreatePropertyGUI(property);

            var helpBox = root.Q<HelpBox>();
            Assert.IsNotNull(helpBox);
            StringAssert.Contains("could not find serialized internal list field", helpBox.text);
        }

        private T CreateHost<T>() where T : ScriptableObject
        {
            var instance = ScriptableObject.CreateInstance<T>();
            createdInstance = instance;
            return instance;
        }
    }
}
