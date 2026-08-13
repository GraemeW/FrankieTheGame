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
            if (!IsValidField(property, out SerializedProperty entriesProperty, out HelpBox helpBox))
            {
                container.Add(helpBox);
                return container;
            }

            Foldout foldout = MakeFoldout(property.displayName);
            for (int i = 0; i < entriesProperty.arraySize; i++)
            {
                SerializedProperty entryProperty = entriesProperty.GetArrayElementAtIndex(i);
                SerializedProperty keyProperty = entryProperty.FindPropertyRelative("key");
                SerializedProperty valueProperty = entryProperty.FindPropertyRelative("value");
                if (keyProperty == null || valueProperty == null) { continue; } // shouldn't happen post-reconciliation, but don't hard-fail the whole drawer over it

                string label = ObjectNames.NicifyVariableName(keyProperty.enumDisplayNames.Length > 0 ? keyProperty.enumDisplayNames[keyProperty.enumValueIndex] : keyProperty.enumNames[keyProperty.enumValueIndex]);

                PropertyField valueField = MakeElementField(valueProperty, label);
                valueField.BindProperty(valueProperty);
                foldout.Add(valueField);
            }

            container.Add(foldout);
            return container;
        }

        private static bool IsValidField(SerializedProperty property, out SerializedProperty entriesProperty, out HelpBox helpBox)
        {
            entriesProperty = null;
            helpBox = new HelpBox();

            object targetObject = property.boxedValue;
            if (targetObject is not IEnumKeyedCollection enumKeyedCollection)
            {
                helpBox = new HelpBox("[EnumKeyedCollection] error: the target field's class must implement IEnumKeyedCollection.", HelpBoxMessageType.Error);
                return false;
            }

            Type enumType = enumKeyedCollection.GetEnumType();
            bool isValidEnum = enumType != null && (enumType.IsEnum || typeof(Enum).IsAssignableFrom(enumType));
            if (!isValidEnum)
            {
                helpBox = new HelpBox("[EnumKeyedCollection] error: invalid or null Enum type returned.", HelpBoxMessageType.Error);
                return false;
            }

            // Reconcile (add missing / drop orphaned / sort into enum order) on the real managed object, then push it back into the SerializedProperty tree
            enumKeyedCollection.SyncEntriesToEnum();
            property.boxedValue = enumKeyedCollection;
            property.serializedObject.ApplyModifiedProperties();

            string listName = enumKeyedCollection.GetListName();
            listName ??= string.Empty;
            entriesProperty = property.FindPropertyRelative(listName);
            if (entriesProperty is not { isArray: true })
            {
                helpBox = new HelpBox($"[EnumKeyedCollection] error: could not find serialized internal list field '{listName}'. Check your field naming.", HelpBoxMessageType.Error);
                return false;
            }
            return true;
        }

        private static PropertyField MakeElementField(SerializedProperty elementProperty, string label)
        {
            return new PropertyField(elementProperty, label)
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
