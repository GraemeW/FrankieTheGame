#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.UIElements;

namespace LowDefMustard.Rendering.Editor
{
    [CustomPropertyDrawer(typeof(BattleEffectRepetition))]
    public sealed class BattleEffectRepetitionDrawer : PropertyDrawer
    {
        // Const Tunables
        private const string _countXPath = BattleEffectRepetition.countField + ".x";
        private const string _countYPath = BattleEffectRepetition.countField + ".y";

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            SerializedProperty modeProperty = property.FindPropertyRelative(BattleEffectRepetition.modeField);
            SerializedProperty mirrorXProperty = property.FindPropertyRelative(BattleEffectRepetition.mirrorXField);
            SerializedProperty mirrorYProperty = property.FindPropertyRelative(BattleEffectRepetition.mirrorYField);
            string countTooltip = property.FindPropertyRelative(BattleEffectRepetition.countField).tooltip;
            var properties = new[] { modeProperty, property.FindPropertyRelative(_countXPath), property.FindPropertyRelative(_countYPath), // Split out x/y for different label/visibility behaviours (dependent on if grid)
                property.FindPropertyRelative(BattleEffectRepetition.spacingSUField), property.FindPropertyRelative(BattleEffectRepetition.copyScaleRatioField), mirrorXProperty, mirrorYProperty }; 

            var section = new BattleEffectSection(property.displayName, property, properties, IsEnabled, refreshSection =>
            {
                bool hasCopies = GetMode() != BattleEffectRepetitionMode.None;
                refreshSection.SetShown(_countXPath, hasCopies);
                refreshSection.SetShown(_countYPath, IsGrid());
                refreshSection.SetLabel(_countXPath, IsGrid() ? "Count X" : "Count");
                refreshSection.SetShown(BattleEffectRepetition.spacingSUField, hasCopies);
                refreshSection.SetShown(BattleEffectRepetition.copyScaleRatioField, hasCopies);
            }, property);
            section.SetLabel(_countXPath, IsGrid() ? "Count X" : "Count", countTooltip);
            section.SetLabel(_countYPath, "Count Y", countTooltip);
            
            return section;

            
            // Local Functions
            BattleEffectRepetitionMode GetMode() => (BattleEffectRepetitionMode)modeProperty.intValue;
            bool IsGrid() => GetMode() is BattleEffectRepetitionMode.Grid or BattleEffectRepetitionMode.StaggeredGrid;
            bool IsEnabled() => GetMode() != BattleEffectRepetitionMode.None || mirrorXProperty.boolValue || mirrorYProperty.boolValue;
        }
    }
}
#endif
