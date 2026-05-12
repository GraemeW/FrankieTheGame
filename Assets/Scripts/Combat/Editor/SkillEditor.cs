using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;
using Frankie.Utils.Localization;

namespace Frankie.Combat.Editor
{
    [CustomEditor(typeof(Skill))]
    public class SkillEditor : UnityEditor.Editor
    {
        private void OnEnable()
        {
            LocalizationTool.InitializeEnglishLocale();
            var skill = (Skill)target;
            if (skill is not ILocalizable localizable) { return; }
            localizable.TryLocalizeStandardEntries(skill, skill.GetPropertyLinkedLocalizationEntries());
        }

        public override VisualElement CreateInspectorGUI() 
        {
            var root = new VisualElement();
            InspectorElement.FillDefaultInspector(root, serializedObject, this);
            return root;
        }
    }
}
