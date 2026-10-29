#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.UIElements;

namespace LowDefMustard.Rendering.Editor
{
    [CustomPropertyDrawer(typeof(BattleEffectBeats))]
    public sealed class BattleEffectBeatsDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            SerializedProperty countProperty = property.FindPropertyRelative(BattleEffectBeats.countField);
            return new BattleEffectSection(property, () => countProperty.intValue > 0, section =>
            {
                int count = countProperty.intValue;
                section.SetShown(BattleEffectBeats.beatAField, count > 0);
                section.SetShown(BattleEffectBeats.beatBField, count > 1);
                section.SetShown(BattleEffectBeats.beatCField, count > 2);
                section.SetShown(BattleEffectBeats.beatDField, count > 3);
                section.SetShown(BattleEffectBeats.patternField, count > 0);
                section.SetShown(BattleEffectBeats.patternRateField, count > 0);
            });
        }
    }
}
#endif
