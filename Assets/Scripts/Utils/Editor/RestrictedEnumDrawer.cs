using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Frankie.Utils.Editor
{
    [CustomPropertyDrawer(typeof(RestrictedEnumAttribute), true)]
    public class RestrictedEnumDrawer : PropertyDrawer
    {
        #region UnityMethods
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!IsValidInput(property, attribute, out RestrictedEnumAttribute restriction))
            {
                EditorGUI.HelpBox(position, $"[RestrictedEnum] on '{property.name}' requires an enum field.", MessageType.Error);
                return;
            }
            
            var hiddenSet = new HashSet<int>(restriction.hiddenValues);
            string[] allNames = property.enumNames;
            int[] allValues = GetEnumValues(property);
            GenerateAllowedNames(hiddenSet, allNames, allValues, out List<string> allowedNames, out List<int> allowedValues);

            int currentEnumSetting = allValues[property.enumValueIndex];
            EditorGUI.BeginProperty(position, label, property);
            EditorGUI.BeginChangeCheck();
            
            int popupIndex = allowedValues.IndexOf(currentEnumSetting);
            if (popupIndex < 0) popupIndex = 0; // Fallback to first allowed value.

            int newPopupIndex = EditorGUI.Popup(position, label.text, popupIndex, allowedNames.ToArray());
            if (EditorGUI.EndChangeCheck())
            {
                SetSerializedValue(property, allowedValues[newPopupIndex], allValues);
            }
            EditorGUI.EndProperty();
        }
        #endregion

        #region PrivateMethods
        private static bool IsValidInput(SerializedProperty serializedProperty, PropertyAttribute propertyAttribute, out RestrictedEnumAttribute restriction)
        {
            restriction = null;
            if (serializedProperty.propertyType != SerializedPropertyType.Enum) { return false; }
            
            restriction = propertyAttribute as RestrictedEnumAttribute;
            if (restriction == null) { return false; }
            return true;
        }

        private static void GenerateAllowedNames(HashSet<int> hiddenSet, string[] allNames, int[] allValues, out List<string> allowedNames, out List<int> allowedValues)
        {
            allowedNames = new List<string>();
            allowedValues = new List<int>();

            for (int i = 0; i < allValues.Length; i++)
            {
                if (!hiddenSet.Contains(allValues[i]))
                {
                    allowedNames.Add(ObjectNames.NicifyVariableName(allNames[i]));
                    allowedValues.Add(allValues[i]);
                }
            }
        }

        private static void SetSerializedValue(SerializedProperty property, int newIntValue, int[] allValues)
        {
            for (int i = 0; i < allValues.Length; i++)
            {
                if (allValues[i] != newIntValue){  continue; }
                property.enumValueIndex = i;
                break;
            }
        }
        
        private static int[] GetEnumValues(SerializedProperty property)
        {
            Type enumType = GetEnumType(property);
            if (enumType == null)
            {
                int count = property.enumNames.Length;
                int[] fallback = new int[count];
                for (int i = 0; i < count; i++)
                {
                    fallback[i] = i;
                }
                return fallback;
            }

            Array values = Enum.GetValues(enumType);
            int[] intVals = new int[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                intVals[i] = (int)values.GetValue(i);
            }
            return intVals;
        }
        
        private static Type GetEnumType(SerializedProperty property)
        {
            Type objectType = property.serializedObject.targetObject.GetType();
            string path = property.propertyPath;
            
            path = System.Text.RegularExpressions.Regex.Replace(path, @"\.Array\.data\[\d+\]", "");

            string[] parts = path.Split('.');
            Type current = objectType;

            foreach (var part in parts)
            {
                var field = current?.GetField(part, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                if (field == null) { return null; }

                current = field.FieldType;
                if (current.IsArray)
                {
                    current = current.GetElementType();
                }
                else if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(List<>))
                {
                    current = current.GetGenericArguments()[0];
                }
            }

            return current is { IsEnum: true } ? current : null;
        }
        #endregion
    }
}
