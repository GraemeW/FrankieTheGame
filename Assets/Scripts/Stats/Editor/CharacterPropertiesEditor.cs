using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;

namespace Frankie.Stats.Editor
{
    [CustomEditor(typeof(CharacterProperties))]
    public class CharacterPropertiesEditor : UnityEditor.Editor
    {
        private void OnEnable()
        {
            var characterProperties = (CharacterProperties)target;
            if (characterProperties == null) { return; }
            characterProperties.TryLocalizedName();
        }

        public override VisualElement CreateInspectorGUI() 
        {
            var root = new VisualElement();
            InspectorElement.FillDefaultInspector(root, serializedObject, this);
            return root;
        }
    }
}
