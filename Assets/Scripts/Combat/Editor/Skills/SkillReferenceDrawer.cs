using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using LowDefMustard.Core;

namespace Frankie.Combat.Editor
{
    [CustomPropertyDrawer(typeof(SkillReferenceAttribute))]
    internal sealed class SkillReferenceDrawer : PropertyDrawer
    {
        // Const Tunables
        private const string _notStringText = "[SkillReference] needs a string field";
        private const string _unresolvableFormat = "Skill '{0}' can't be referenced by name (it's not in the Skill addressables group, or its name is shared) ~ kept the previous skill.";

        // Classes
        internal const string skillFieldClass = "skill-reference-drawer__field";

        #region UnityMethods
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            if (property.propertyType != SerializedPropertyType.String) { return new Label(_notStringText); }

            string label = string.IsNullOrEmpty(preferredLabel) ? property.displayName : preferredLabel;
            var objectField = new ObjectField(label) { objectType = typeof(Skill), allowSceneObjects = false, tooltip = property.tooltip };
            objectField.AddToClassList(BaseField<UnityEngine.Object>.alignedFieldUssClassName);
            objectField.AddToClassList(skillFieldClass);
            objectField.SetValueWithoutNotify(GetSkill(property));

            SerializedObject serializedObject = property.serializedObject;
            string propertyPath = property.propertyPath;
            objectField.RegisterValueChangedCallback(changeEvent => OnSkillChanged(objectField, serializedObject, propertyPath, changeEvent.newValue as Skill));
            objectField.TrackPropertyValue(property, changedProperty => objectField.SetValueWithoutNotify(GetSkill(changedProperty))); // e.g. undo / the Skill Set Editor
            return objectField;
        }
        #endregion

        #region PrivateMethods
        private static Skill GetSkill(SerializedProperty property) => Skill.GetSkillFromName(property.stringValue);

        private static void OnSkillChanged(ObjectField objectField, SerializedObject serializedObject, string propertyPath, Skill skill)
        {
            serializedObject.Update();
            SerializedProperty property = serializedObject.FindProperty(propertyPath); // Re-find via path on each change ~ list elements resolve to whatever entry is at that index now
            if (property == null) { return; }
            if (skill != null && Skill.GetSkillFromName(skill.name) != skill)
            {
                LDMLog.Warning(LDMLogCategory.Combat, string.Format(_unresolvableFormat, skill.name), skill);
                objectField.SetValueWithoutNotify(GetSkill(property));
                return;
            }

            property.stringValue = skill != null ? skill.name : string.Empty;
            serializedObject.ApplyModifiedProperties();
        }
        #endregion
    }
}
