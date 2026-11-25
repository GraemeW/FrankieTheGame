using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace LowDefMustard.Rendering.Editor
{
    [CustomPropertyDrawer(typeof(GraphicsWarmupEntry))]
    public sealed class GraphicsWarmupEntryDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            // Set Up
            SerializedProperty pathProperty = property.FindPropertyRelative(GraphicsWarmupEntry.pathField);
            var foldout = new Foldout { text = property.displayName, value = property.isExpanded };
            foldout.Add(new PropertyField(pathProperty));
            PropertyField materialField = AddField(GraphicsWarmupEntry.materialField);
            PropertyField filterField = AddField(GraphicsWarmupEntry.filterField);
            PropertyField panelSettingsField = AddField(GraphicsWarmupEntry.panelSettingsField);
            PropertyField targetTextureField = AddField(GraphicsWarmupEntry.targetTextureField);
            RefreshFields();

            // Registration
            foldout.RegisterValueChangedCallback(changeEvent =>
            {
                if (changeEvent.target != foldout) { return; } // i.e. ignore value changes bubbling up from the child fields
                property.isExpanded = changeEvent.newValue;
            });
            foldout.TrackPropertyValue(pathProperty, _ => RefreshFields());

            return foldout;


            // Local Functions
            PropertyField AddField(string fieldName)
            {
                var field = new PropertyField(property.FindPropertyRelative(fieldName));
                foldout.Add(field);
                return field;
            }

            void RefreshFields()
            {
                if (property.serializedObject.FindProperty(property.propertyPath) == null) { return; } // i.e. if its item is deleted, holds until the inspector rebuilds
                var path = (GraphicsWarmupPath)pathProperty.enumValueIndex;
                SetShown(materialField, GraphicsWarmupEntry.UsesMaterial(path));
                SetShown(filterField, GraphicsWarmupEntry.UsesFilter(path));
                SetShown(panelSettingsField, GraphicsWarmupEntry.UsesPanelSettings(path));
                SetShown(targetTextureField, GraphicsWarmupEntry.UsesTargetTexture(path));
            }

            static void SetShown(VisualElement field, bool isShown) => field.style.display = isShown ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
