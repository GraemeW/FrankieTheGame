#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.UIElements;

namespace LowDefMustard.Rendering.Editor
{
    [CustomPropertyDrawer(typeof(BattleEffectAltColours))]
    public sealed class BattleEffectAltColoursDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            SerializedProperty countProperty = property.FindPropertyRelative(BattleEffectAltColours.countField);
            return new BattleEffectSection(property, () => countProperty.intValue > 0, section =>
            {
                int count = countProperty.intValue;
                section.SetShown(BattleEffectAltColours.colourBField, count > 0);
                section.SetShown(BattleEffectAltColours.colourCField, count > 1);
                section.SetShown(BattleEffectAltColours.colourDField, count > 2);
            });
        }
    }
}
#endif
