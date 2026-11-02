#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.UIElements;

namespace LowDefMustard.Rendering.Editor
{
    [CustomPropertyDrawer(typeof(BattleEffectTiling))]
    public sealed class BattleEffectTilingDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            SerializedProperty modeProperty = property.FindPropertyRelative(BattleEffectTiling.modeField);
            SerializedProperty isAnchoredProperty = property.FindPropertyRelative(BattleEffectTiling.isAnchoredField);
            return new BattleEffectSection(property, IsEnabled, section =>
            {
                section.SetShown(BattleEffectTiling.countField, IsEnabled());
                section.SetShown(BattleEffectTiling.spacingEUField, IsEnabled());
                section.SetShown(BattleEffectTiling.isAnchoredField, IsEnabled());
                section.SetShown(BattleEffectTiling.anchorEUField, IsEnabled() && isAnchoredProperty.boolValue);
            });

            // Local Functions
            bool IsEnabled() => modeProperty.intValue != (int)BattleEffectTilingMode.None;
        }
    }
}
#endif
