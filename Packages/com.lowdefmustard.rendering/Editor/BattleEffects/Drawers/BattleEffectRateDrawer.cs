#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine.UIElements;

namespace LowDefMustard.Rendering.Editor
{
    [CustomPropertyDrawer(typeof(BattleEffectJitter))]
    [CustomPropertyDrawer(typeof(BattleEffectFlicker))]
    [CustomPropertyDrawer(typeof(BattleEffectPulse))]
    public sealed class BattleEffectRateDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            string rateFieldName = property.type switch
            {
                nameof(BattleEffectFlicker) => BattleEffectFlicker.rateField,
                nameof(BattleEffectPulse) => BattleEffectPulse.rateField,
                _ => BattleEffectJitter.rateField
            };
            SerializedProperty rateProperty = property.FindPropertyRelative(rateFieldName);
            
            string[] settingFieldNames = BattleEffectSection.GetChildren(property).Select(child => child.name).Where(name => name != rateFieldName).ToArray();
            return new BattleEffectSection(property, IsOn, section =>
            {
                foreach (string settingFieldName in settingFieldNames) { section.SetShown(settingFieldName, IsOn()); }
            });

            
            // Local Functions
            bool IsOn() => rateProperty.floatValue > 0f;
        }
    }
}
#endif
