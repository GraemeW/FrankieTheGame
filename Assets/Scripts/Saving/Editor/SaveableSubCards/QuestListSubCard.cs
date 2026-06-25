using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using Frankie.Quests;

namespace Frankie.Saving.Editor
{
    public class QuestListSubCard : SaveableSubCardData
    {
        public QuestListSubCard(ISaveableBase saveable, SaveState saveState)
        {
            this.saveable = saveable;
            this.saveState = saveState;
        }

        protected override void AddEditableFieldsToSubCardView(Box subCardView)
        {
            if (saveable is not QuestList questList) { return; }
            
            List<QuestStatus> questStatuses = questList.ManualGetDataFromState(saveState);
            questStatuses ??= new List<QuestStatus>();

            var listContainer = new VisualElement();
            subCardView.Add(listContainer);

            var buttonRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            subCardView.Add(buttonRow);

            var addButton = new Button { text = "+ Add Quest" , style = { width = standardButtonWidth } };
            buttonRow.Add(addButton);

            DrawQuestStatusList(listContainer, questList, questStatuses);

            addButton.RegisterCallback<ClickEvent>(_ =>
            {
                questStatuses.Add(new QuestStatus(null));
                saveState = questList.ManualGetStateFromData(questStatuses);
                RaiseSaveStateChanged();
                DrawQuestStatusList(listContainer, questList, questStatuses);
            });
        }

        private void DrawQuestStatusList(VisualElement listContainer, QuestList questList, List<QuestStatus> questStatuses)
        {
            listContainer.Clear();

            if (questStatuses.Count == 0)
            {
                listContainer.Add(new Label("No questList save data found"));
                return;
            }

            for (int i = 0; i < questStatuses.Count; i++)
            {
                int questIndex = i;
                QuestStatus questStatus = questStatuses[questIndex];

                var questCard = new Box();
                listContainer.Add(questCard);

                var questRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };
                questCard.Add(questRow);

                questRow.Add(new Label($"Quest {questIndex}:") { style = { width = 120, unityTextAlign = TextAnchor.MiddleLeft } });

                var questField = new ObjectField { objectType = typeof(Quest), value = questStatus.GetQuest(), style = { flexGrow = 1 } };
                questRow.Add(questField);

                var removeQuestButton = new Button { text = "- Remove Quest", style = { width = smallButtonWidth } };
                questRow.Add(removeQuestButton);

                var objectivesContainer = new VisualElement { style = { marginLeft = 8 } };
                questCard.Add(objectivesContainer);
                DrawObjectivesForQuestStatus(objectivesContainer, questList, questStatuses, questIndex);

                questField.RegisterValueChangedCallback(changeEvent =>
                {
                    var newQuest = changeEvent.newValue as Quest;
                    questStatuses[questIndex] = new QuestStatus(newQuest);
                    saveState = questList.ManualGetStateFromData(questStatuses);
                    RaiseSaveStateChanged();
                    DrawObjectivesForQuestStatus(objectivesContainer, questList, questStatuses, questIndex);
                });

                removeQuestButton.RegisterCallback<ClickEvent>(_ =>
                {
                    questStatuses.RemoveAt(questIndex);
                    saveState = questList.ManualGetStateFromData(questStatuses);
                    RaiseSaveStateChanged();
                    DrawQuestStatusList(listContainer, questList, questStatuses);
                });
            }
        }

        private void DrawObjectivesForQuestStatus(VisualElement objectivesContainer, QuestList questList, List<QuestStatus> questStatuses, int questIndex)
        {
            objectivesContainer.Clear();

            QuestStatus questStatus = questStatuses[questIndex];
            Quest quest = questStatus.GetQuest();
            if (quest == null) { return; }

            foreach (QuestObjective questObjective in quest.GetObjectives())
            {
                var objectiveRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };
                objectivesContainer.Add(objectiveRow);

                bool isComplete = questStatus.GetStatusForObjective(questObjective);
                var completeField = new Toggle { value = isComplete };
                objectiveRow.Add(completeField);
                objectiveRow.Add(new Label(questObjective.name) { style = { unityTextAlign = TextAnchor.MiddleLeft } });

                completeField.RegisterValueChangedCallback(changeEvent =>
                {
                    questStatus.SetObjective(questObjective, changeEvent.newValue);
                    saveState = questList.ManualGetStateFromData(questStatuses);
                    RaiseSaveStateChanged();
                });
            }
        }
    }
}
