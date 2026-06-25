using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using Frankie.Stats;

namespace Frankie.Saving.Editor
{
    public class PartyAssistSubCard : SaveableSubCardData
    {
        private readonly SaveableEntityCardData parentSaveableEntityCardData;
        
        public PartyAssistSubCard(ISaveableBase saveable, SaveState saveState, SaveableEntityCardData parentSaveableEntityCardData)
        {
            this.saveable = saveable;
            this.saveState = saveState;
            this.parentSaveableEntityCardData = parentSaveableEntityCardData;
        }

        public override void AddEditableFieldsToSubCardView(Box subCardView)
        {
            if (saveable is not PartyAssist partyAssist) { return; }
            
            List<CharacterProperties> partyAssistSaveData = partyAssist.ManualGetDataFromState(saveState);
            if (partyAssistSaveData == null)
            {
                subCardView.Add(new Label("No PartyAssist save data found"));
                return;
            }

            // Section 1 - Party Assist Character Select
            var partyAssistCharacters = new List<CharacterProperties>(partyAssistSaveData);
            
            var listContainer = new VisualElement();
            subCardView.Add(listContainer);

            var addButton = new Button { text = "+ Add Character", style = { width = standardButtonWidth } };
            subCardView.Add(addButton);

            DrawPartyAssistList(listContainer, partyAssist, partyAssistCharacters);

            addButton.RegisterCallback<ClickEvent>(_ =>
            {
                partyAssistCharacters.Add(null);
                saveState = partyAssist.ManualGetStateFromData(partyAssistCharacters);
                RaiseSaveStateChanged();
                DrawPartyAssistList(listContainer, partyAssist, partyAssistCharacters);
            });
            
            // Section 2 - Party Entity View
            if (parentSaveableEntityCardData == null) { return; }
            var characterSaveableEntityCards = new List<SaveableEntityCardData>();
            foreach (CharacterProperties partyCharacter in partyAssistCharacters)
            {
                if (partyCharacter == null || partyCharacter.GetCharacterPrefab() == null) { continue; }

                SaveableEntityCardData characterSaveableEntityCard = parentSaveableEntityCardData.BuildFromCharacterPropertiesWithCache(partyCharacter);
                if (characterSaveableEntityCard == null) { continue; }
                characterSaveableEntityCards.Add(characterSaveableEntityCard);
            }

            foreach (SaveableEntityCardData characterSaveableEntityCard in characterSaveableEntityCards)
            {
                // TODO:  Draw view using methodology established in InactivePartySubCard
            }
        }

        private void DrawPartyAssistList(VisualElement listContainer, PartyAssist partyAssist, List<CharacterProperties> partyAssistCharacters)
        {
            listContainer.Clear();

            for (int i = 0; i < partyAssistCharacters.Count; i++)
            {
                int rowIndex = i;

                var row = new VisualElement { style = { flexDirection = FlexDirection.Row } };
                listContainer.Add(row);

                var characterField = new ObjectField { objectType = typeof(CharacterProperties), value = partyAssistCharacters[rowIndex], style = { flexGrow = 1 } };
                row.Add(characterField);

                var removeButton = new Button { text = "- Remove", style = { width = smallButtonWidth }  };
                row.Add(removeButton);

                characterField.RegisterValueChangedCallback(changeEvent =>
                {
                    var newCharacterProperties = changeEvent.newValue as CharacterProperties;
                    if (newCharacterProperties != null && newCharacterProperties.GetCharacterPrefab() == null)
                    {
                        Debug.LogWarning("Invalid Character:  Select entry with a character prefab.");
                        characterField.SetValueWithoutNotify(partyAssistCharacters[rowIndex]);
                        return;
                    }
                    
                    partyAssistCharacters[rowIndex] = newCharacterProperties;
                    saveState = partyAssist.ManualGetStateFromData(partyAssistCharacters);
                    RaiseSaveStateChanged();
                });

                removeButton.RegisterCallback<ClickEvent>(_ =>
                {
                    partyAssistCharacters.RemoveAt(rowIndex);
                    saveState = partyAssist.ManualGetStateFromData(partyAssistCharacters);
                    RaiseSaveStateChanged();
                    DrawPartyAssistList(listContainer, partyAssist, partyAssistCharacters);
                });
            }
        }
    }
}
