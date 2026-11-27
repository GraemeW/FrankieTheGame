using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Unity.Scripting.LifecycleManagement;
using LowDefMustard.Utils.Editor;

namespace LowDefMustard.Utils.Tests.Editor
{
    public class SerializedFieldUtilityTests
    {
        // Tested:
        //  - GetSerializedFields - public, [SerializeField], [SerializeReference];  base class private fields;  skips private, [NonSerialized], readonly, const, static
        //  - IsSerialized - via GetSerializedFields
        //  - GetCollectionElementType - T[], List<T> unwrap;  other types (incl. other generics) returned as-is
        //  - IsInlineSerializable - [Serializable] class / struct;  primitives, enums, strings, Object references, abstract, non-[Serializable], core library types
        // Not Tested:
        //  - Unity's own serialization edge cases (e.g. generic [Serializable] class instances, depth limits) - not mirrored

        #region DataStructures
        private enum TestEnum { A }
        [Serializable] private class InlineClass { public int value; }
        [Serializable] private struct InlineStruct { public int value; }
        [Serializable] private abstract class AbstractInline { public int value; }
        private class NotSerializable { public int value; }

        private class BaseHost : ScriptableObject
        {
            [SerializeField] private int baseSerialized;
            private int basePrivate;
            
            internal const string baseSerializedField = nameof(baseSerialized);
            public int GetBaseValues() => baseSerialized + basePrivate; // Keeps the fields in use
        }

        private class Host : BaseHost
        {
            public int publicField;
            [SerializeField] private int serializedPrivate;
            [SerializeReference] private object managedReference;
            private int privateField;
            [NonSerialized] public int nonSerialized;
            public readonly int readOnly;
            public const int constant = 1;
            [NoAutoStaticsCleanup] public static int staticField;
            
            internal const string serializedPrivateField = nameof(serializedPrivate);
            internal const string managedReferenceField = nameof(managedReference);
            public int GetValues() => serializedPrivate + privateField + (managedReference == null ? 0 : 1); // Keeps the fields in use
        }
        #endregion

        #region Tests
        [Test]
        public void GetSerializedFields_Host_SerializedOnlyIncludingBase()
        {
            string[] fieldNames = SerializedFieldUtility.GetSerializedFields(typeof(Host)).Select(field => field.Name).ToArray();

            CollectionAssert.AreEquivalent(new[] { nameof(Host.publicField), Host.serializedPrivateField, Host.managedReferenceField, BaseHost.baseSerializedField }, fieldNames);
        }

        [TestCase(typeof(int[]), typeof(int))]
        [TestCase(typeof(List<string>), typeof(string))]
        [TestCase(typeof(InlineClass), typeof(InlineClass))]
        [TestCase(typeof(Dictionary<int, int>), typeof(Dictionary<int, int>))]
        public void GetCollectionElementType_Type_UnwrapsArraysAndLists(Type fieldType, Type expected)
        {
            Assert.AreEqual(expected, SerializedFieldUtility.GetCollectionElementType(fieldType));
        }

        [TestCase(typeof(InlineClass), true)]
        [TestCase(typeof(InlineStruct), true)]
        [TestCase(typeof(int), false)]
        [TestCase(typeof(TestEnum), false)]
        [TestCase(typeof(string), false)]
        [TestCase(typeof(ScriptableObject), false)]
        [TestCase(typeof(AbstractInline), false)]
        [TestCase(typeof(NotSerializable), false)]
        [TestCase(typeof(Dictionary<int, int>), false)]
        public void IsInlineSerializable_Type_MatchesInlineData(Type type, bool expected)
        {
            Assert.AreEqual(expected, SerializedFieldUtility.IsInlineSerializable(type));
        }
        #endregion
    }
}
