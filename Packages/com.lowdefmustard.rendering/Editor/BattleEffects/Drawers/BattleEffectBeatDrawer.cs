#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.UIElements;

namespace LowDefMustard.Rendering.Editor
{
    [CustomPropertyDrawer(typeof(BattleEffectBeat))]
    public sealed class BattleEffectBeatDrawer : PropertyDrawer
    {
        // Note: owned by the parent beats (its pattern hides the start time)

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            SerializedProperty beats = property.serializedObject.FindProperty(property.propertyPath[..property.propertyPath.LastIndexOf('.')]);
            SerializedProperty pattern = beats.FindPropertyRelative(BattleEffectBeats.patternField);
            SerializedProperty patternRate = beats.FindPropertyRelative(BattleEffectBeats.patternRateField);
            SerializedProperty isInterpolated = beats.FindPropertyRelative(BattleEffectBeats.isInterpolatedField);
            string startField = $"{property.name}.{BattleEffectBeat.startSecondsField}";
            return new BattleEffectSection(property.displayName, beats, BattleEffectSection.GetChildren(property),
                refreshFields: section => section.SetShown(startField, isInterpolated.boolValue || !BattleEffectBeats.IsPatternUsed(pattern.stringValue, patternRate.floatValue)),
                foldProperty: property);
        }
    }
}
#endif
