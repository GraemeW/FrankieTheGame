#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.UIElements;

namespace LowDefMustard.Rendering.Editor
{
    [CustomPropertyDrawer(typeof(BattleEffectShade))]
    public sealed class BattleEffectShadeDrawer : PropertyDrawer
    {
        // Note:  the item's style decides which steps apply (fill:  no stroke;  outline:  no fill;  fill and outline:  a stroke step only w/ its outline from the palette)

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            SerializedProperty enabledProperty = property.FindPropertyRelative(BattleEffectShade.isEnabledField);
            SerializedProperty styleProperty = FindSiblingStyle(property);
            SerializedProperty modeProperty = styleProperty?.FindPropertyRelative(BattleEffectStyle.modeField);
            SerializedProperty fromPaletteProperty = styleProperty?.FindPropertyRelative(BattleEffectStyle.isOutlineFromPaletteField);
            var shadeSection = new BattleEffectSection(property, () => enabledProperty.boolValue, section =>
            {
                bool isEnabled = enabledProperty.boolValue;
                section.SetShown(BattleEffectShade.angleDegreesField, isEnabled);
                section.SetShown(BattleEffectShade.offsetSUField, isEnabled);
                section.SetShown(BattleEffectShade.isSmoothField, isEnabled);

                // w/o a sibling style (outside an item):  both steps
                var mode = modeProperty == null ? BattleEffectStyleMode.FillAndOutline : (BattleEffectStyleMode)modeProperty.intValue;
                bool isFromPalette = fromPaletteProperty == null || fromPaletteProperty.boolValue;
                section.SetShown(BattleEffectShade.fillPaletteStepField, isEnabled && mode != BattleEffectStyleMode.Outline);
                section.SetShown(BattleEffectShade.outlinePaletteStepField, isEnabled && (mode == BattleEffectStyleMode.Outline || (mode == BattleEffectStyleMode.FillAndOutline && isFromPalette)));
            });
            if (styleProperty != null) { shadeSection.TrackAlso(styleProperty); }
            return shadeSection;
        }

        private static SerializedProperty FindSiblingStyle(SerializedProperty property)
        {
            int lastDot = property.propertyPath.LastIndexOf('.');
            string stylePath = lastDot < 0 ? BattleEffectItem.styleField : $"{property.propertyPath[..lastDot]}.{BattleEffectItem.styleField}";
            return property.serializedObject.FindProperty(stylePath);
        }
    }
}
#endif
