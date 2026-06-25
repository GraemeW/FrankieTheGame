using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;
using Frankie.Stats;
using UnityEditor.UIElements;
using UnityEngine;

namespace Frankie.Saving.Editor
{
    public class InactivePartySubCard : SaveableSubCardData
    {
        private readonly SaveableEntityCardData parentSaveableEntityCardData;
        
        public InactivePartySubCard(ISaveableBase saveable, SaveState saveState, SaveableEntityCardData parentSaveableEntityCardData)
        {
            this.saveable = saveable;
            this.saveState = saveState;
            this.parentSaveableEntityCardData = parentSaveableEntityCardData;
        }

        public override void AddEditableFieldsToSubCardView(Box subCardView)
        {
            if (saveable is not InactiveParty inactiveParty) { return; }
            
            HashSet<CharacterProperties> inactivePartyData = inactiveParty.ManualGetDataFromState(saveState);
            if (inactivePartyData == null)
            {
                subCardView.Add(new Label("No InactiveParty save data found"));
                return;
            }

            // Section 1 - Inactive Party Character Select
            var inactivePartyCharacters = new List<CharacterProperties>(inactivePartyData);
            
            var listContainer = new VisualElement();
            subCardView.Add(listContainer);

            var addButton = new Button { text = "+ Add Character", style = { width = standardButtonWidth } };
            subCardView.Add(addButton);

            DrawInactivePartyList(listContainer, inactiveParty, inactivePartyCharacters);

            addButton.RegisterCallback<ClickEvent>(_ =>
            {
                inactivePartyCharacters.Add(null);
                saveState = inactiveParty.ManualGetStateFromData(inactivePartyCharacters.ToHashSet());
                RaiseSaveStateChanged();
                DrawInactivePartyList(listContainer, inactiveParty, inactivePartyCharacters);
            });
            
            // Section 2 - Party Entity View
            List<(CharacterProperties characterProperties, SaveableEntityCardData entityCardData)> inactivePartyEntries = BuildInactivePartySaveableEntityData(inactivePartyData);
            foreach ((CharacterProperties characterProperties, SaveableEntityCardData inactiveCharacterEntry) in inactivePartyEntries)
            {
                var entityBox = new Box();
                subCardView.Add(entityBox);
                
            }
        }

        
        private void DrawInactivePartyList(VisualElement listContainer, InactiveParty inactiveParty, List<CharacterProperties> inactivePartyCharacters)
        {
            listContainer.Clear();

            for (int i = 0; i < inactivePartyCharacters.Count; i++)
            {
                int rowIndex = i;

                var row = new VisualElement { style = { flexDirection = FlexDirection.Row } };
                listContainer.Add(row);

                var characterField = new ObjectField { objectType = typeof(CharacterProperties), value = inactivePartyCharacters[rowIndex], style = { flexGrow = 1 } };
                row.Add(characterField);

                var removeButton = new Button { text = "- Remove", style = { width = smallButtonWidth }  };
                row.Add(removeButton);

                characterField.RegisterValueChangedCallback(changeEvent =>
                {
                    var newCharacterProperties = changeEvent.newValue as CharacterProperties;
                    if (newCharacterProperties != null && newCharacterProperties.GetCharacterPrefab() == null)
                    {
                        Debug.LogWarning("Invalid Character:  Select entry with a character prefab.");
                        characterField.SetValueWithoutNotify(inactivePartyCharacters[rowIndex]);
                        return;
                    }
                    
                    inactivePartyCharacters[rowIndex] = newCharacterProperties;
                    saveState = inactiveParty.ManualGetStateFromData(inactivePartyCharacters.ToHashSet());
                    RaiseSaveStateChanged();
                });

                removeButton.RegisterCallback<ClickEvent>(_ =>
                {
                    inactivePartyCharacters.RemoveAt(rowIndex);
                    saveState = inactiveParty.ManualGetStateFromData(inactivePartyCharacters.ToHashSet());
                    RaiseSaveStateChanged();
                    DrawInactivePartyList(listContainer, inactiveParty, inactivePartyCharacters);
                });
            }
        }
        
        private List<(CharacterProperties characterProperties, SaveableEntityCardData entityCardData)> BuildInactivePartySaveableEntityData(HashSet<CharacterProperties> inactivePartyData)
        {
            var inactivePartyEntries = new List<(CharacterProperties characterProperties, SaveableEntityCardData entityCardData)>();
            if (parentSaveableEntityCardData == null) { return inactivePartyEntries; }
            
            foreach (CharacterProperties characterProperties in inactivePartyData)
            {
                SaveableEntityCardData characterSaveableEntityData = parentSaveableEntityCardData.BuildFromCharacterPropertiesWithCache(characterProperties);
                inactivePartyEntries.Add((characterProperties, characterSaveableEntityData));
            }
            return inactivePartyEntries;
        }
    }
}
