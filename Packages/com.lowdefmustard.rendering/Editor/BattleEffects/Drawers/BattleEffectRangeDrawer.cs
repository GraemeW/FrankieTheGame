#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace LowDefMustard.Rendering.Editor
{
    [CustomPropertyDrawer(typeof(BattleEffectFloatRange))]
    [CustomPropertyDrawer(typeof(BattleEffectVector2Range))]
    public sealed class BattleEffectRangeDrawer : PropertyDrawer
    {
        // Const Tunables
        private const string _arrow = "→";
        private const float _arrowMargin = 3f;

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            if (property.type == nameof(BattleEffectVector2Range))
            {
                return BuildRow(new Vector2Field(property.displayName), new Vector2Field(), property,
                    property.FindPropertyRelative(BattleEffectVector2Range.startField), property.FindPropertyRelative(BattleEffectVector2Range.endField));
            }
            return BuildRow(new FloatField(property.displayName), new FloatField(), property,
                property.FindPropertyRelative(BattleEffectFloatRange.startField), property.FindPropertyRelative(BattleEffectFloatRange.endField));
        }

        private static VisualElement BuildRow<T>(BaseField<T> startField, BaseField<T> endField, SerializedProperty property, SerializedProperty start, SerializedProperty end)
        {
            // Set Up
            startField.AddToClassList(BaseField<T>.alignedFieldUssClassName);
            startField.tooltip = property.tooltip;
            startField.BindProperty(start);
            endField.BindProperty(end);
            endField.style.flexGrow = 1f;
            endField.style.flexShrink = 1f;

            startField.Add(new Label(_arrow) { style = { unityTextAlign = TextAnchor.MiddleCenter, marginLeft = _arrowMargin, marginRight = _arrowMargin } });
            startField.Add(endField);

            // Registration
            List<VisualElement> startInputs = GetInputs(startField, endField);
            List<VisualElement> endInputs = GetInputs(endField, null);
            RefreshColours();
            startField.TrackPropertyValue(property, _ => RefreshColours());
            return startField;

            
            // Local Functions
            void RefreshColours()
            {
                if (property.serializedObject.FindProperty(property.propertyPath) == null) { return; } // i.e. its item was deleted, holds until the inspector rebuilds
                bool[] doesValueChange = start.propertyType == SerializedPropertyType.Vector2
                    ? new[] { !Mathf.Approximately(start.vector2Value.x, end.vector2Value.x), !Mathf.Approximately(start.vector2Value.y, end.vector2Value.y) }
                    : new[] { !Mathf.Approximately(start.floatValue, end.floatValue) };
                
                for (int i = 0; i < doesValueChange.Length; i++)
                {
                    StyleColor colour = doesValueChange[i] ? BattleEffectInspectorColours.changingInput : StyleKeyword.Null;
                    if (i < startInputs.Count) { startInputs[i].style.backgroundColor = colour; }
                    if (i < endInputs.Count) { endInputs[i].style.backgroundColor = colour; }
                }
            }
        }

        private static List<VisualElement> GetInputs(VisualElement field, VisualElement excluded)
        {
            return field.Query(className: TextInputBaseField<float>.inputUssClassName).Where(input => excluded == null || !excluded.Contains(input)).ToList();
        }
    }
}
#endif
