using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace LowDefMustard.Utils.Editor
{
    [CustomPropertyDrawer(typeof(EnumKeyedCollectionAttribute))]
    public class EnumKeyedCollectionDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var container = new VisualElement();
            if (!IsValidField(property, out Type enumType, out SerializedProperty arrayProperty, out HelpBox helpBox))
            {
                container.Add(helpBox);
                return container;
            }
            
            // Enforce EnumKeyedCollection to max enumeration size
            string[] enumNames = Enum.GetNames(enumType);
            int enumSize = enumNames.Length;

            if (arrayProperty.arraySize != enumSize)
            {
                arrayProperty.arraySize = enumSize;
                arrayProperty.serializedObject.ApplyModifiedProperties();
            }
            
            // Force enumerate into enumeration elements
            Foldout foldout = MakeFoldout(property.displayName);
            for (int i = 0; i < enumSize; i++)
            {
                SerializedProperty elementProperty = arrayProperty.GetArrayElementAtIndex(i);
                PropertyField elementField = MakeElementField(elementProperty, enumNames[i]);
                elementField.BindProperty(elementProperty);
                foldout.Add(elementField);
            }

            container.Add(foldout);
            return container;
        }

        private static bool IsValidField(SerializedProperty property, out Type enumType, out SerializedProperty arrayProperty, out HelpBox helpBox)
        {
            enumType = null;
            arrayProperty = null;
            helpBox = new HelpBox();
            
            object targetObject = property.boxedValue;
            if (targetObject is not IEnumKeyedCollection enumProvider)
            {
                helpBox = new HelpBox($"[EnumList] error: The target field class must implement IEnumProvider.", HelpBoxMessageType.Error);
                return false;
            }

            enumType = enumProvider.GetEnumType();
            bool isValidEnum = enumType != null && (enumType.IsEnum || typeof(Enum).IsAssignableFrom(enumType));
            if (!isValidEnum)
            {
                helpBox = new HelpBox($"[EnumList] error: Invalid or null Enum type returned.", HelpBoxMessageType.Error);
                return false;
            }

            string listName = enumProvider.GetListName();
            listName ??= string.Empty;
            arrayProperty = property.FindPropertyRelative(listName);
            if (arrayProperty is not { isArray: true })
            {
                helpBox = new HelpBox($"[EnumList] error: Could not find serialized internal list field 'zones'. Check your field naming.", HelpBoxMessageType.Error);
                return false;
            }
            return true;
        }

        private static PropertyField MakeElementField(SerializedProperty elementProperty, string enumName)
        {
            return new PropertyField(elementProperty, enumName)
            {
                style =
                {
                    marginLeft = 15,
                    marginTop = 2,
                    marginBottom = 2
                }
            };
        }

        private static Foldout MakeFoldout(string label)
        {
            return new Foldout 
            { 
                text = label, 
                value = true,
                style =
                {
                    marginLeft = 3
                }
            };
        }
    }
}
