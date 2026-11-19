using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Frankie.Combat.Editor
{
    [CustomPropertyDrawer(typeof(SkillSet), true)]
    public class SkillSetDrawer : PropertyDrawer
    {
        // Const Tunables
        private const string _openText = "Open";
        private const string _openTooltip = "Open in Skill Set Editor";

        // Classes
        internal const string openButtonClass = "skill-set-drawer__open-button";

        #region UnityMethods
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row } };

            string label = string.IsNullOrEmpty(preferredLabel) ? property.displayName : preferredLabel;
            var objectField = new ObjectField(label) { objectType = fieldInfo?.FieldType ?? typeof(SkillSet), tooltip = property.tooltip, style = { flexGrow = 1f, flexShrink = 1f } };
            objectField.AddToClassList(BaseField<UnityEngine.Object>.alignedFieldUssClassName);
            objectField.BindProperty(property);
            row.Add(objectField);

            var openButton = new Button(() => { if (objectField.value is SkillSet skillSet) { SkillSetEditor.ShowSkillSet(skillSet); } }) { text = _openText, tooltip = _openTooltip, style = { flexShrink = 0f } };
            openButton.AddToClassList(openButtonClass);
            row.Add(openButton);

            RefreshOpenButton(openButton, property.objectReferenceValue);
            objectField.RegisterValueChangedCallback(changeEvent => RefreshOpenButton(openButton, changeEvent.newValue)); // Bound ~ also fires on undo / external edits
            return row;
        }
        #endregion

        #region PrivateMethods
        private static void RefreshOpenButton(Button openButton, UnityEngine.Object skillSet) => openButton.style.display = skillSet is SkillSet ? DisplayStyle.Flex : DisplayStyle.None;
        #endregion
    }
}
