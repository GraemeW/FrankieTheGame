using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;
using Frankie.Utils.Localization;

namespace Frankie.Stats.Editor
{
    [CustomEditor(typeof(CharacterProperties))]
    public class CharacterPropertiesEditor : UnityEditor.Editor
    {
        private void OnEnable()
        {
            LocalizationTool.InitializeEnglishLocale();
            var characterProperties = (CharacterProperties)target;
            if (characterProperties is not ILocalizable localizable) { return; }
            localizable.TryLocalizeStandardEntries(characterProperties, characterProperties.GetPropertyLinkedLocalizationEntries());
        }

        public override VisualElement CreateInspectorGUI() 
        {
            var root = new VisualElement();
            InspectorElement.FillDefaultInspector(root, serializedObject, this);
            return root;
        }
    }
}
