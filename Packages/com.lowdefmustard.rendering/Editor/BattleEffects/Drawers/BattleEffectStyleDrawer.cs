#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.UIElements;

namespace LowDefMustard.Rendering.Editor
{
    [CustomPropertyDrawer(typeof(BattleEffectStyle))]
    public sealed class BattleEffectStyleDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            SerializedProperty modeProperty = property.FindPropertyRelative(BattleEffectStyle.modeField);
            property.isExpanded = true;
            return new BattleEffectSection(property, null, section =>
            {
                var mode = (BattleEffectStyleMode)modeProperty.intValue;
                section.SetShown(BattleEffectStyle.outlineThicknessEUField, mode != BattleEffectStyleMode.Fill);
                section.SetShown(BattleEffectStyle.outlineColourField, mode == BattleEffectStyleMode.FillAndOutline);
            });
        }
    }
}
#endif
