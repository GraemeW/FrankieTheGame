using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace LowDefMustard.Utils.Editor
{
    [CustomPropertyDrawer(typeof(RestrictedEnumAttribute), true)]
    public class RestrictedEnumDrawer : PropertyDrawer
    {
        // Const/Static Tunables
        private const string _overrideStyleClass = "restricted-enum-drawer__prefab-override";
        private static readonly Color _overrideBarColor = new Color(0.11f, 0.53f, 0.93f);
        
        #region UnityMethods
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            if (!IsValidInput(property, attribute, out RestrictedEnumAttribute restriction))
            {
                return new HelpBox($"[RestrictedEnum] on '{property.name}' requires an enum field.", HelpBoxMessageType.Error);
            }

            var hiddenSet = new HashSet<int>(restriction.hiddenValues);
            string[] allNames = property.enumNames;
            int[] allValues = GetEnumValues(property, fieldInfo);
            GenerateAllowedNames(hiddenSet, allNames, allValues, out List<string> allowedNames, out List<int> allowedValues);

            int popupIndex = ResolvePopupIndex(property, allowedValues);

            var dropdown = new DropdownField(property.displayName, allowedNames, popupIndex);
            dropdown.RegisterValueChangedCallback(evt => OnDropdownValueChanged(evt, property, allowedNames, allowedValues));

            // Keep the dropdown in sync with external changes (undo/redo, multi-editing, other inspectors)
            dropdown.TrackPropertyValue(property, changedProperty =>
            {
                SyncDropdownToProperty(dropdown, changedProperty, allowedValues, allowedNames);
                RefreshPrefabOverrideState(dropdown, changedProperty);
            });

            RegisterPrefabOverrideSupport(dropdown, property, allowedValues, allowedNames);

            return dropdown;
        }
        #endregion

        #region UIToolkitHelpers
        // ResolvePopup marked internal so fallback-indexing logic can be tested directly
        internal static int ResolvePopupIndex(SerializedProperty property, List<int> allowedValues)
        {
            int popupIndex = allowedValues.IndexOf(property.intValue);
            return popupIndex < 0 ? 0 : popupIndex; // Fallback to first allowed value.
        }

        private static void OnDropdownValueChanged(ChangeEvent<string> evt, SerializedProperty property, List<string> allowedNames, List<int> allowedValues)
        {
            int newIndex = allowedNames.IndexOf(evt.newValue);
            if (newIndex < 0) { return; }

            property.serializedObject.Update();
            SetSerializedValue(property, allowedValues[newIndex]);
            property.serializedObject.ApplyModifiedProperties();
        }

        private static void SyncDropdownToProperty(DropdownField dropdown, SerializedProperty changedProperty, List<int> allowedValues, List<string> allowedNames)
        {
            int updatedIndex = ResolvePopupIndex(changedProperty, allowedValues);
            dropdown.SetValueWithoutNotify(allowedNames[updatedIndex]);
        }
        #endregion

        #region PrefabOverrideSupport
        // Plain DropdownField doesn't automatically pick up Unity's built-in prefab-override bar or the right-click "Revert"/"Apply" menu
        // Reproduce both manually against the same SerializedProperty
        private static void RegisterPrefabOverrideSupport(DropdownField dropdown, SerializedProperty property, List<int> allowedValues, List<string> allowedNames)
        {
            RefreshPrefabOverrideState(dropdown, property);
            dropdown.AddManipulator(new ContextualMenuManipulator(evt => BuildPrefabOverrideMenu(evt, dropdown, property, allowedValues, allowedNames)));
        }

        private static void RefreshPrefabOverrideState(DropdownField dropdown, SerializedProperty property)
        {
            bool isOverridden = !property.serializedObject.isEditingMultipleObjects && property.prefabOverride;
            dropdown.EnableInClassList(_overrideStyleClass, isOverridden);
            dropdown.style.borderLeftWidth = isOverridden ? 2 : 0;
            dropdown.style.borderLeftColor = isOverridden ? _overrideBarColor : Color.clear;
        }

        private static void BuildPrefabOverrideMenu(ContextualMenuPopulateEvent evt, DropdownField dropdown, SerializedProperty property, List<int> allowedValues, List<string> allowedNames)
        {
            if (property.serializedObject.isEditingMultipleObjects) { return; }
            if (!property.prefabOverride) { return; }

            UnityEngine.Object target = property.serializedObject.targetObject;
            if (!PrefabUtility.IsPartOfPrefabInstance(target)) { return; }

            evt.menu.AppendAction("Revert Value to Prefab", _ =>
            {
                PrefabUtility.RevertPropertyOverride(property, InteractionMode.UserAction);
                property.serializedObject.Update();
                SyncDropdownToProperty(dropdown, property, allowedValues, allowedNames);
                RefreshPrefabOverrideState(dropdown, property);
            });

            UnityEngine.Object sourceObject = PrefabUtility.GetCorrespondingObjectFromSource(target);
            string prefabAssetPath = sourceObject != null ? AssetDatabase.GetAssetPath(sourceObject) : null;
            if (string.IsNullOrEmpty(prefabAssetPath)) { return; }

            string prefabName = System.IO.Path.GetFileNameWithoutExtension(prefabAssetPath);
            evt.menu.AppendAction($"Apply to Prefab '{prefabName}'", _ =>
            {
                PrefabUtility.ApplyPropertyOverride(property, prefabAssetPath, InteractionMode.UserAction);
                RefreshPrefabOverrideState(dropdown, property);
            });
        }
        #endregion

        #region InternalHelpers
        // Helpers marked internal so LowDefMustard.Utils.Tests.Editor can test directly
        internal static bool IsValidInput(SerializedProperty serializedProperty, PropertyAttribute propertyAttribute, out RestrictedEnumAttribute restriction)
        {
            restriction = null;
            if (serializedProperty.propertyType != SerializedPropertyType.Enum) { return false; }
            
            restriction = propertyAttribute as RestrictedEnumAttribute;
            if (restriction == null) { return false; }
            return true;
        }

        internal static void GenerateAllowedNames(HashSet<int> hiddenSet, string[] allNames, int[] allValues, out List<string> allowedNames, out List<int> allowedValues)
        {
            allowedNames = new List<string>();
            allowedValues = new List<int>();

            for (int i = 0; i < allValues.Length; i++)
            {
                if (hiddenSet.Contains(allValues[i])) { continue; }
                allowedNames.Add(ObjectNames.NicifyVariableName(allNames[i]));
                allowedValues.Add(allValues[i]);
            }
        }

        internal static void SetSerializedValue(SerializedProperty property, int newIntValue)
        {
            property.intValue = newIntValue;
        }
        
        internal static int[] GetEnumValues(SerializedProperty property, FieldInfo fieldInfo)
        {
            Type enumType = GetEnumType(fieldInfo);
            if (enumType == null)
            {
                int count = property.enumNames.Length;
                int[] fallback = new int[count];
                for (int i = 0; i < count; i++) { fallback[i] = i; }
                return fallback;
            }

            Array values = Enum.GetValues(enumType);
            int[] intVals = new int[values.Length];
            for (int i = 0; i < values.Length; i++) { intVals[i] = (int)values.GetValue(i); }
            return intVals;
        }

        internal static Type GetEnumType(FieldInfo fieldInfo)
        {
            if (fieldInfo == null) { return null; }

            Type type = fieldInfo.FieldType;
            if (type.IsArray) { type = type.GetElementType(); }
            else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                type = type.GetGenericArguments()[0];
            }

            return type is { IsEnum: true } ? type : null;
        }
        #endregion
    }
}
