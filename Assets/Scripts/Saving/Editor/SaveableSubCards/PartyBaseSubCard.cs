using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using Frankie.Stats;

namespace Frankie.Saving.Editor
{
    public abstract class PartyBaseSubCard : SaveableSubCardData
    {
        // State
        protected readonly Dictionary<CharacterProperties, SaveableEntityCardData> characterSaveableEntityCards = new();
        
        // UI State
        protected VisualElement characterEntityContainer;
        
        protected void ReconcileEntityView(List<CharacterProperties> updatedCharacterList)
        {
            RebuildCharacterSaveableEntityCards(updatedCharacterList);
            DrawCharacterEntityView(characterEntityContainer);
        }
        
        protected void DrawBasicPartyList(VisualElement listContainer, ISaveable<HashSet<CharacterProperties>> basicPartySaveable, List<CharacterProperties> partyCharacters, Action drawCallback)
        {
            listContainer.Clear();

            for (int i = 0; i < partyCharacters.Count; i++)
            {
                int rowIndex = i;

                var row = new VisualElement { style = { flexDirection = FlexDirection.Row } };
                listContainer.Add(row);

                var characterField = new ObjectField { objectType = typeof(CharacterProperties), value = partyCharacters[rowIndex], style = { flexGrow = 1 } };
                row.Add(characterField);

                var removeButton = new Button { text = "- Remove", style = { width = smallButtonWidth }  };
                row.Add(removeButton);

                characterField.RegisterValueChangedCallback(changeEvent =>
                {
                    var newCharacterProperties = changeEvent.newValue as CharacterProperties;
                    if (newCharacterProperties != null && newCharacterProperties.GetCharacterPrefab() == null)
                    {
                        Debug.LogWarning("Invalid Character:  Select entry with a character prefab.");
                        characterField.SetValueWithoutNotify(partyCharacters[rowIndex]);
                        return;
                    }
                    
                    partyCharacters[rowIndex] = newCharacterProperties;
                    saveState = basicPartySaveable.ManualGetStateFromData(partyCharacters.ToHashSet());
                    RaiseSaveStateChanged();
                    drawCallback?.Invoke();
                });

                removeButton.RegisterCallback<ClickEvent>(_ =>
                {
                    partyCharacters.RemoveAt(rowIndex);
                    saveState = basicPartySaveable.ManualGetStateFromData(partyCharacters.ToHashSet());
                    RaiseSaveStateChanged();
                    drawCallback?.Invoke();
                });
            }
        }
        
        private void RebuildCharacterSaveableEntityCards(List<CharacterProperties> checkCharacterProperties)
        {
            foreach (CharacterProperties characterProperties in checkCharacterProperties)
            {
                if (characterProperties == null || characterProperties.GetCharacterPrefab() == null) { continue; }
                if (characterSaveableEntityCards.ContainsKey(characterProperties)) { continue; }
                
                SaveableEntityCardData characterSaveableEntityCard = saveableEntityCardData.BuildFromCharacterPropertiesWithCache(characterProperties);
                if (characterSaveableEntityCard == null) { continue; }
                characterSaveableEntityCards[characterProperties] = characterSaveableEntityCard;
            }
        }

        private void DrawCharacterEntityView(VisualElement container)
        {
            characterEntityContainer.Clear();
            foreach (SaveableEntityCardData characterSaveableEntityCard in characterSaveableEntityCards.Values)
            { 
                Box entityCardView = characterSaveableEntityCard.DrawSaveableEntityCard(() => characterSaveableEntityCard.SaveSaveableEntity(true));
                container.Add(entityCardView);
                container.Add(new VisualElement { style = { height = entityCardSpacerHeight } });
            }
        }
    }
}
