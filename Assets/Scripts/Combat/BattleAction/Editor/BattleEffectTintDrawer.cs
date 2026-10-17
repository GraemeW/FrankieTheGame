#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Frankie.Combat.Editor
{
    [CustomPropertyDrawer(typeof(BattleEffectTint))]
    public sealed class BattleEffectTintDrawer : PropertyDrawer
    {
        // Note:  Default layout, w/ only the fields the selected mode uses shown

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            // Set Up
            var foldout = new Foldout { text = property.displayName, value = property.isExpanded };
            SerializedProperty modeProperty = property.FindPropertyRelative(BattleEffectTint.modeField);
            var fields = new Dictionary<string, PropertyField>();
            SerializedProperty child = property.Copy();
            SerializedProperty end = property.GetEndProperty();
            for (bool hasChild = child.NextVisible(true); hasChild && !SerializedProperty.EqualContents(child, end); hasChild = child.NextVisible(false))
            {
                var field = new PropertyField(child.Copy());
                fields[child.name] = field;
                foldout.Add(field);
            }
            RefreshFields();

            // Registration
            foldout.RegisterValueChangedCallback(changeEvent =>
            {
                if (changeEvent.target != foldout) { return; }
                property.isExpanded = changeEvent.newValue;
            });
            foldout.TrackPropertyValue(modeProperty, _ => RefreshFields());

            return foldout;


            // Local Functions
            void RefreshFields()
            {
                var mode = (BattleEffectTintMode)modeProperty.intValue;
                SetShown(BattleEffectTint.colourBField, mode != BattleEffectTintMode.Solid);
                SetShown(BattleEffectTint.angleField, mode is BattleEffectTintMode.Linear or BattleEffectTintMode.Wave);
                SetShown(BattleEffectTint.frequencyField, mode != BattleEffectTintMode.Solid);
                SetShown(BattleEffectTint.speedField, mode is BattleEffectTintMode.Wave or BattleEffectTintMode.Radial);
            }

            void SetShown(string fieldName, bool isShown)
            {
                if (fields.TryGetValue(fieldName, out PropertyField field)) { field.style.display = isShown ? DisplayStyle.Flex : DisplayStyle.None; }
            }
        }
    }
}
#endif
