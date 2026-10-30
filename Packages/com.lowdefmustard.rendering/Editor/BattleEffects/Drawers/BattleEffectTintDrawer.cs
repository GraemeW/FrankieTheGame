#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.UIElements;

namespace LowDefMustard.Rendering.Editor
{
    [CustomPropertyDrawer(typeof(BattleEffectTint))]
    public sealed class BattleEffectTintDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            SerializedProperty modeProperty = property.FindPropertyRelative(BattleEffectTint.modeField);
            return new BattleEffectSection(property, null, section =>
            {
                var mode = (BattleEffectTintMode)modeProperty.intValue;
                section.SetShown(BattleEffectTint.colourBField, mode != BattleEffectTintMode.Solid);
                section.SetShown(BattleEffectTint.angleDegreesField, mode is BattleEffectTintMode.Linear or BattleEffectTintMode.Wave);
                section.SetShown(BattleEffectTint.bandsPerEUField, mode != BattleEffectTintMode.Solid);
                section.SetShown(BattleEffectTint.scrollRateField, mode is BattleEffectTintMode.Wave or BattleEffectTintMode.Radial);
            });
        }
    }
}
#endif
