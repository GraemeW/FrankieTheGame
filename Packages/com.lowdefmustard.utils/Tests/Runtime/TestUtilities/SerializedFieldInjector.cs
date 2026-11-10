#if UNITY_EDITOR
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LowDefMustard.Utils.Tests
{
    public static class SerializedFieldInjector
    {
        // Sets a private [SerializeField] in the same manner that the inspector would

        public static void Set(Object target, string fieldName, Object value)
        {
            using var serializedObject = new SerializedObject(target);
            GetProperty(serializedObject, target, fieldName).objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetArray(Object target, string fieldName, params Object[] values)
        {
            using var serializedObject = new SerializedObject(target);
            SerializedProperty property = GetProperty(serializedObject, target, fieldName);
            Assert.IsTrue(property.isArray, $"{target.GetType().Name}.{fieldName} is not an array/list");
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) { property.GetArrayElementAtIndex(i).objectReferenceValue = values[i]; }
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static SerializedProperty GetProperty(SerializedObject serializedObject, Object target, string fieldName)
        {
            SerializedProperty property = serializedObject.FindProperty(fieldName);
            Assert.IsNotNull(property, $"{target.GetType().Name} has no serialized field {fieldName}");
            return property;
        }
    }
}
#endif
