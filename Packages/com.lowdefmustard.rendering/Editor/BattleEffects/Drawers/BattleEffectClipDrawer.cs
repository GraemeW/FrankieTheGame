#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.UIElements;

namespace LowDefMustard.Rendering.Editor
{
    [CustomPropertyDrawer(typeof(BattleEffectClip))]
    public sealed class BattleEffectClipDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            SerializedProperty shapeProperty = property.FindPropertyRelative(BattleEffectClip.shapeField);
            return new BattleEffectSection(property, IsEnabled, section =>
            {
                section.SetShown(BattleEffectClip.halfSizeSUField, IsEnabled());
                section.SetShown(BattleEffectClip.angleDegreesField, IsEnabled());
            });

            // Local Functions
            bool IsEnabled() => shapeProperty.intValue != (int)BattleEffectClipShape.None;
        }
    }
}
#endif
