using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using LowDefMustard.Localization;
using Frankie.Utils.Localization;

namespace Frankie.Core.Editor
{
    [CustomEditor(typeof(PlayerQuestion))]
    public class PlayerQuestionEditor : UnityEditor.Editor
    {
        private void OnEnable()
        {
            LocalizationLocale.TriggerLocalizationSettingsInitialization();
            var playerQuestion = (PlayerQuestion)target;
            if (playerQuestion is not ILocalizable localizable) { return; }
            localizable.TryLocalizeStandardEntries(playerQuestion, playerQuestion.GetPropertyLinkedLocalizationEntries());
        }

        public override VisualElement CreateInspectorGUI() 
        {
            var root = new VisualElement();
            InspectorElement.FillDefaultInspector(root, serializedObject, this);
            return root;
        }
    }
}
