using UnityEditor;
using UnityEngine.UIElements;
using Frankie.Core.GameStateModifiers;
using Frankie.Utils.Localization;

namespace Frankie.Quests.Editor
{
    [CustomEditor(typeof(Quest))]
    public class QuestEditor : GameStateModifierEditor
    {
        private const string _questHeaderTitle = "Quest Functionality";
        private const string _buttonGenerateObjectiveText = "Generate Objectives (Save to Take Effect)";
        
        private const float _fontSize = 14;
        private const float _headerMarginTop = 8;
        private const float _headerMarginBottom = 4;
        
        protected override void OnEnable()
        {
            base.OnEnable();
            
            LocalizationTool.InitializeEnglishLocale();
            var quest = (Quest)target;
            if (quest is not ILocalizable localizable) { return; }
            localizable.TryLocalizeStandardEntries(quest, quest.GetPropertyLinkedLocalizationEntries());
        }
        
        public override VisualElement CreateInspectorGUI()
        {
            VisualElement root = base.CreateInspectorGUI();
            root.Add(MakeQuestHeader(_questHeaderTitle));

            var generateButton = new Button { text = _buttonGenerateObjectiveText };
            generateButton.RegisterCallback<ClickEvent>(OnGenerateObjectiveClick);
            root.Add(generateButton);

            return root;
        }

        private void OnGenerateObjectiveClick(ClickEvent clickEvent)
        {
            var quest = (Quest)target;
            if (quest == null) { return; }
            quest.GenerateObjectiveFromNames();
        }

        private static VisualElement MakeQuestHeader(string headerTitle)
        {
            var section = new VisualElement
            {
                style =
                {
                    marginTop = _headerMarginTop,
                    marginBottom = _headerMarginBottom
                }
            };
            var title = new Label(headerTitle)
            {
                style =
                {
                    fontSize = _fontSize,
                    unityFontStyleAndWeight = UnityEngine.FontStyle.Bold
                }
            };
            section.Add(title);
            return section;
        }
    }
}
