#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace LowDefMustard.Rendering.Editor
{
    [CustomPropertyDrawer(typeof(BattleEffectBeats))]
    public sealed class BattleEffectBeatsDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            // Set Up
            var foldout = new Foldout { text = property.displayName, value = property.isExpanded };
            SerializedProperty countProperty = property.FindPropertyRelative(BattleEffectBeats.countField);
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
            foldout.TrackPropertyValue(countProperty, _ => RefreshFields());

            return foldout;


            // Local Functions
            void RefreshFields()
            {
                int count = countProperty.intValue;
                SetShown(BattleEffectBeats.beatAField, count > 0);
                SetShown(BattleEffectBeats.beatBField, count > 1);
                SetShown(BattleEffectBeats.beatCField, count > 2);
                SetShown(BattleEffectBeats.beatDField, count > 3);
            }

            void SetShown(string fieldName, bool isShown)
            {
                if (fields.TryGetValue(fieldName, out PropertyField field)) { field.style.display = isShown ? DisplayStyle.Flex : DisplayStyle.None; }
            }
        }
    }
}
#endif
