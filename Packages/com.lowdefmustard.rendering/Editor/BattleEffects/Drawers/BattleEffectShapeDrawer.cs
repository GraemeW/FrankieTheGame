#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace LowDefMustard.Rendering.Editor
{
    [CustomPropertyDrawer(typeof(BattleEffectShape), true)]
    public sealed class BattleEffectShapeDrawer : PropertyDrawer
    {
        // Notes:
        //  - Every BattleEffectShape subclass is listed to ensure new shapes don't need editor changes
        //  - Settings marked [BattleEffectShownWhen] show only while their controlling setting holds one of its values
        //  - Right-click functionality enabled on label to copy / paste the shape's settings (BattleEffectClipboard)

        // Const Tunables
        private const float _settingsIndent = 15f;

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            // Set Up
            var root = new VisualElement();
            var typeField = new DropdownField(property.displayName, BattleEffectShapeTypes.GetNames(), 0);
            typeField.SetValueWithoutNotify(BattleEffectShapeTypes.Format(GetShapeType()));
            typeField.AddToClassList(BaseField<string>.alignedFieldUssClassName);
            var settings = new VisualElement { style = { marginLeft = _settingsIndent } };
            var conditionalFields = new List<(PropertyField field, SerializedProperty controller, BattleEffectShownWhenAttribute condition)>();
            root.Add(typeField);
            root.Add(settings);
            RebuildSettings();

            // Registration
            typeField.RegisterValueChangedCallback(changeEvent =>
            {
                if (!BattleEffectShapeTypes.TrySetShape(property, changeEvent.newValue)) { return; }
                RebuildSettings();
            });
            root.TrackPropertyValue(property, _ =>
            {
                // e.g. undo / redo swapping the shape type
                if (typeField.value == BattleEffectShapeTypes.Format(GetShapeType())) { return; }
                typeField.SetValueWithoutNotify(BattleEffectShapeTypes.Format(GetShapeType()));
                RebuildSettings();
            });
            root.TrackSerializedObjectValue(property.serializedObject, _ => RefreshConditionalFields()); // Whole object:  property tracking inside a [SerializeReference] does not fire properly
            BattleEffectClipboard.AddMenu(typeField.labelElement, "Shape", property, new[] { property });

            return root;


            // Local Functions
            Type GetShapeType() => property.managedReferenceValue?.GetType();

            void RebuildSettings()
            {
                settings.Clear();
                conditionalFields.Clear();
                Type shapeType = GetShapeType();
                foreach (SerializedProperty child in BattleEffectSection.GetChildren(property))
                {
                    // Setup property field -> add to visual element (bind below)
                    var field = new PropertyField(child);
                    settings.Add(field);
                    
                    // Initialize conditional entry (refresh below)
                    FieldInfo localFieldInfo = shapeType?.GetField(child.name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    var condition = localFieldInfo?.GetCustomAttribute<BattleEffectShownWhenAttribute>();
                    SerializedProperty controller = condition != null ? property.FindPropertyRelative(condition.fieldName) : null;
                    if (controller != null) { conditionalFields.Add((field, controller, condition)); }
                }
                
                RefreshConditionalFields();
                settings.Bind(property.serializedObject);
            }

            void RefreshConditionalFields()
            {
                if (property.serializedObject.FindProperty(property.propertyPath) == null) { return; } // i.e. if its item is deleted, holds until the inspector rebuilds
                foreach ((PropertyField field, SerializedProperty controller, BattleEffectShownWhenAttribute condition) in conditionalFields)
                {
                    field.style.display = condition.IsShownFor(controller.intValue) ? DisplayStyle.Flex : DisplayStyle.None;
                }
            }
        }
    }
}
#endif
