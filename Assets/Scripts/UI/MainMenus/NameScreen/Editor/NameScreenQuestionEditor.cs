using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using Frankie.Utils.Localization;

namespace Frankie.Menu.UI.Editor
{
    [CustomEditor(typeof(NameScreenQuestion))]
    public class NameScreenQuestionEditor : UnityEditor.Editor
    {
        private void OnEnable()
        {
            LocalizationTool.InitializeEnglishLocale();
            var nameScreenQuestion = (NameScreenQuestion)target;
            if (nameScreenQuestion is not ILocalizable localizable) { return; }
            localizable.TryLocalizeStandardEntries(nameScreenQuestion, nameScreenQuestion.GetPropertyLinkedLocalizationEntries());
        }

        public override VisualElement CreateInspectorGUI() 
        {
            var root = new VisualElement();
            InspectorElement.FillDefaultInspector(root, serializedObject, this);
            return root;
        }
    }
}
