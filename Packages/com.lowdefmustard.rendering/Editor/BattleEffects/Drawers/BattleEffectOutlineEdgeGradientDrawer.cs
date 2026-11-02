#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.UIElements;

namespace LowDefMustard.Rendering.Editor
{
    [CustomPropertyDrawer(typeof(BattleEffectOutlineEdgeGradient))]
    public sealed class BattleEffectOutlineEdgeGradientDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            SerializedProperty enabledProperty = property.FindPropertyRelative(BattleEffectOutlineEdgeGradient.isEnabledField);
            SerializedProperty fromPaletteProperty = property.FindPropertyRelative(BattleEffectOutlineEdgeGradient.isFromPaletteField);
            return new BattleEffectSection(property, () => enabledProperty.boolValue, section =>
            {
                section.SetShown(BattleEffectOutlineEdgeGradient.colourField, enabledProperty.boolValue && !fromPaletteProperty.boolValue);
                section.SetShown(BattleEffectOutlineEdgeGradient.stepsField, enabledProperty.boolValue);
                section.SetShown(BattleEffectOutlineEdgeGradient.isFromPaletteField, enabledProperty.boolValue);
                section.SetShown(BattleEffectOutlineEdgeGradient.paletteStepField, enabledProperty.boolValue && fromPaletteProperty.boolValue);
            });
        }
    }
}
#endif
