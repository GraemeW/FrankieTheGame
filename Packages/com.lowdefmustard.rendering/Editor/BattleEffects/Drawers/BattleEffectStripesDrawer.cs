#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.UIElements;

namespace LowDefMustard.Rendering.Editor
{
    [CustomPropertyDrawer(typeof(BattleEffectStripes))]
    public sealed class BattleEffectStripesDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            SerializedProperty stripesProperty = property.FindPropertyRelative(BattleEffectStripes.stripesPerEUField);
            return new BattleEffectSection(property, IsEnabled, section =>
            {
                section.SetShown(BattleEffectStripes.angleDegreesField, IsEnabled());
                section.SetShown(BattleEffectStripes.dutyFractionField, IsEnabled());
                section.SetShown(BattleEffectStripes.phaseFractionField, IsEnabled());
            });

            // Local Functions
            bool IsEnabled() => stripesProperty.floatValue > 0f;
        }
    }
}
#endif
