#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.UIElements;

namespace LowDefMustard.Rendering.Editor
{
    [CustomPropertyDrawer(typeof(BattleEffectColourCycle))]
    public sealed class BattleEffectColourCycleDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            SerializedProperty sourceProperty = property.FindPropertyRelative(BattleEffectColourCycle.sourceField);
            SerializedProperty rateProperty = property.FindPropertyRelative(BattleEffectColourCycle.rateField);
            return new BattleEffectSection(property, IsEnabled, section =>
            {
                section.SetShown(BattleEffectColourCycle.rateField, !IsCopySource());
                section.SetShown(BattleEffectColourCycle.phaseOffsetField, IsEnabled());
            });

            // Local Functions
            bool IsCopySource() => sourceProperty.enumValueIndex == (int)BattleEffectColourSource.Copy;
            bool IsEnabled() => IsCopySource() || rateProperty.floatValue > 0f;
        }
    }
}
#endif
