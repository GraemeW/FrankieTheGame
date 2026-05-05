using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;

namespace Frankie.Combat.Editor
{
    [CustomEditor(typeof(Skill))]
    public class SkillEditor : UnityEditor.Editor
    {
        private void OnEnable()
        {
            var skill = (Skill)target;
            if (skill == null) { return; }
            skill.TryLocalizeDefaults();
        }

        public override VisualElement CreateInspectorGUI() 
        {
            var root = new VisualElement();
            InspectorElement.FillDefaultInspector(root, serializedObject, this);
            return root;
        }
    }
}
