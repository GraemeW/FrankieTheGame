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
            return new BattleEffectSection(property, () => enabledProperty.boolValue, section =>
            {
                section.SetShown(BattleEffectOutlineEdgeGradient.colourField, enabledProperty.boolValue);
                section.SetShown(BattleEffectOutlineEdgeGradient.stepsField, enabledProperty.boolValue);
            });
        }
    }
}
#endif
