#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
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
        private const string _xLabel = "X";
        private const string _yLabel = "Y";
        private const float _arrowMargin = 3f;
        private const float _axisLabelWidth = 12f;
        private const float _axisLabelExtraLetterWidth = 8f;
        private const float _axisFieldMinWidth = 14f;
        private const float _axisFieldMargin = 4f;

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            if (property.type == nameof(BattleEffectVector2Range))
            {
                VisualElement row = BuildRow(new Vector2Field(property.displayName), new Vector2Field(), property, 
                    property.FindPropertyRelative(BattleEffectVector2Range.startField), property.FindPropertyRelative(BattleEffectVector2Range.endField));
                TrackAxisLabels(row, property, fieldInfo?.GetCustomAttribute<BattleEffectAxisLabelsWhenAttribute>());
                return row;
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
            AllowShrinking(startField, endField);

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

        private static void AllowShrinking<T>(BaseField<T> startField, BaseField<T> endField)
        {
            bool isComposite = startField is not FloatField;
            foreach (BaseField<T> field in new[] { startField, endField })
            {
                VisualElement input = field.Q(className: BaseField<T>.inputUssClassName);
                input.style.flexBasis = 0f;
                input.style.flexGrow = 1f;
                input.style.flexShrink = 1f;
                input.style.minWidth = isComposite ? 2f * (_axisFieldMinWidth + _axisFieldMargin) : _axisFieldMinWidth;
                if (!isComposite) { continue; }
                
                input.Query(className: BaseCompositeField<Vector2, FloatField, float>.spacerUssClassName).ForEach(spacer => spacer.style.display = DisplayStyle.None);
                input.Query<FloatField>().ForEach(axisField =>
                {
                    axisField.style.minWidth = _axisFieldMinWidth;
                    axisField.style.flexShrink = 1f;
                    axisField.labelElement.style.minWidth = _axisLabelWidth;
                    axisField.labelElement.style.width = _axisLabelWidth;
                    axisField.Q(className: BaseField<float>.inputUssClassName).style.minWidth = 0f;
                });
            }
            endField.style.flexBasis = 0f;
            endField.style.flexGrow = 1f;
            endField.style.flexShrink = 1f;
            endField.style.minWidth = 0f;
        }

        private static void TrackAxisLabels(VisualElement row, SerializedProperty property, BattleEffectAxisLabelsWhenAttribute condition)
        {
            if (condition == null) { return; }
            
            SerializedProperty controller = property.serializedObject.FindProperty($"{property.propertyPath[..(property.propertyPath.LastIndexOf('.') + 1)]}{condition.fieldName}");
            RefreshLabels();
            row.TrackPropertyValue(controller, _ => RefreshLabels());
            return;
            
            
            // Local Functions
            void RefreshLabels()
            {
                if (property.serializedObject.FindProperty(property.propertyPath) == null) { return; } // i.e. its item was deleted, holds until the inspector rebuilds
                bool isRelabelled = controller.intValue == condition.value;
                int letters = isRelabelled ? Mathf.Max(condition.xLabel.Length, condition.yLabel.Length) : 1;
                float labelWidth = _axisLabelWidth + _axisLabelExtraLetterWidth * (letters - 1); // Both axes alike, keeping their inputs lined up
                int axis = 0;
                row.Query<FloatField>().ForEach(axisField =>
                {
                    bool isX = axis++ % 2 == 0; // Each Vector2Field's x, y in order (start, then end)
                    axisField.label = isRelabelled ? (isX ? condition.xLabel : condition.yLabel) : (isX ? _xLabel : _yLabel);
                    axisField.labelElement.style.width = labelWidth;
                    axisField.labelElement.style.minWidth = labelWidth;
                    axisField.labelElement.style.flexShrink = 0f;
                });
            }
        }

        private static List<VisualElement> GetInputs(VisualElement field, VisualElement excluded)
        {
            return field.Query(className: TextInputBaseField<float>.inputUssClassName).Where(input => excluded == null || !excluded.Contains(input)).ToList();
        }
    }
}
#endif
