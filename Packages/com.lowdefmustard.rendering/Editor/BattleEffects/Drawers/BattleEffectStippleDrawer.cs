#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.UIElements;

namespace LowDefMustard.Rendering.Editor
{
    [CustomPropertyDrawer(typeof(BattleEffectStipple))]
    public sealed class BattleEffectStippleDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            SerializedProperty dotsProperty = property.FindPropertyRelative(BattleEffectStipple.dotsPerEUField);
            return new BattleEffectSection(property, IsEnabled, section =>
            {
                section.SetShown(BattleEffectStipple.patternField, IsEnabled());
                section.SetShown(BattleEffectStipple.aspectRatioField, IsEnabled());
                section.SetShown(BattleEffectStipple.densityField, IsEnabled());
                section.SetShown(BattleEffectStipple.seedField, IsEnabled());
            });

            // Local Functions
            bool IsEnabled() => dotsProperty.floatValue > 0f;
        }
    }
}
#endif
