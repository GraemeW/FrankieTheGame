#if UNITY_EDITOR
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LowDefMustard.Rendering.Tests
{
    internal static class SerializedFieldInjector
    {
        // Use this for setting a private [SerializeField] in the same manner that the inspector would

        public static void Set(Object target, string fieldName, Object value)
        {
            var serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(fieldName);
            Assert.IsNotNull(property, $"{target.GetType().Name} has no serialized field {fieldName}");
            property.objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
