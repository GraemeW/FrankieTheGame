#if UNITY_EDITOR
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
            // The end field + arrow sit inside the aligned start field (after its input) to ensure the label lines up w/ the rest of the inspector
            startField.AddToClassList(BaseField<T>.alignedFieldUssClassName);
            startField.tooltip = property.tooltip;
            startField.BindProperty(start);
            endField.BindProperty(end);
            endField.style.flexGrow = 1f;
            endField.style.flexShrink = 1f;

            startField.Add(new Label(_arrow) { style = { unityTextAlign = TextAnchor.MiddleCenter, marginLeft = _arrowMargin, marginRight = _arrowMargin } });
            startField.Add(endField);
            return startField;
        }
    }
}
#endif
