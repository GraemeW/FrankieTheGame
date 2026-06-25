using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using Frankie.Stats;

namespace Frankie.Saving.Editor
{
    public class PartySubCard : PartyBaseSubCard
    {
        public PartySubCard(ISaveableBase saveable, SaveState saveState, SaveableEntityCardData parentSaveableEntityCardData)
        {
            this.saveable = saveable;
            this.saveState = saveState;
            this.parentSaveableEntityCardData = parentSaveableEntityCardData;
        }

        protected override void AddEditableFieldsToSubCardView(Box subCardView)
        {
            if (saveable is not Party party) { return; }
            
            PartySaveData partySaveData = party.ManualGetDataFromState(saveState);
            if (partySaveData == null)
            {
                subCardView.Add(new Label("No Party save data found"));
                return;
            }

            List<CharacterProperties> partyCharacters = new List<CharacterProperties>(partySaveData.partyCharacters);
            List<CharacterProperties> unlockedCharacters = new List<CharacterProperties>(partySaveData.unlockedCharacters);
            List<(CharacterProperties characterProperties, SceneParentReferencePair sceneParentReferencePair)> worldNPCRows =
                partySaveData.worldNPCLookup.Select(pair => (pair.Key, pair.Value)).ToList();

            // Section 1 -- Party Character Select
            subCardView.Add(new Label("Party Members"));
            var partyCharactersContainer = new VisualElement();
            subCardView.Add(partyCharactersContainer);
            
            var addPartyCharacterButton = new Button { text = "+ Add Party Member", style = { width = standardButtonWidth }  };
            subCardView.Add(addPartyCharacterButton);
            DrawPartyCharacterList(partyCharactersContainer, partyCharacters, PushSaveState);
            
            // Section 2 -- Unlocked Characters
            subCardView.Add(new Label("Unlocked Characters"));
            var unlockedCharactersContainer = new VisualElement();
            subCardView.Add(unlockedCharactersContainer);

            var addUnlockedCharacterButton = new Button { text = "+ Add Unlocked Character", style = { width = standardButtonWidth }  };
            subCardView.Add(addUnlockedCharacterButton);
            DrawUnlockedCharacterList(unlockedCharactersContainer, unlockedCharacters, PushSaveState);
            
            // Section 3 -- World NPC Lookup
            subCardView.Add(new Label("World NPC Lookup"));
            var worldNPCContainer = new VisualElement();
            subCardView.Add(worldNPCContainer);

            var addWorldNPCButton = new Button { text = "+ Add World NPC Entry", style = { width = standardButtonWidth } };
            subCardView.Add(addWorldNPCButton);
            DrawWorldNPCList(worldNPCContainer, worldNPCRows, PushSaveState);
            
            // Section 4 -- Party Entity View
            subCardView.Add(new Label("Party Entity View"));
            characterEntityContainer = new VisualElement();
            subCardView.Add(characterEntityContainer);
            
            if (parentSaveableEntityCardData == null) { return; }
            ReconcileEntityView(partyCharacters);
            
            // Button Callbacks
            addPartyCharacterButton.RegisterCallback<ClickEvent>(_ =>
            {
                partyCharacters.Add(null);
                PushSaveState(true);
                DrawPartyCharacterList(partyCharactersContainer, partyCharacters, PushSaveState);
            });
            
            addUnlockedCharacterButton.RegisterCallback<ClickEvent>(_ =>
            {
                unlockedCharacters.Add(null);
                PushSaveState(false);
                DrawUnlockedCharacterList(unlockedCharactersContainer, unlockedCharacters, PushSaveState);
            });
            
            addWorldNPCButton.RegisterCallback<ClickEvent>(_ =>
            {
                worldNPCRows.Add((null, new SceneParentReferencePair(string.Empty, string.Empty)));
                PushSaveState(false);
                DrawWorldNPCList(worldNPCContainer, worldNPCRows, PushSaveState);
            });
            
            return;

            // Local Functions
            void PushSaveState(bool redrawCharacterEntityView)
            {
                var unlockedCharactersSet = new HashSet<CharacterProperties>(unlockedCharacters);
                var worldNPCLookup = new Dictionary<CharacterProperties, SceneParentReferencePair>();
                foreach ((CharacterProperties characterProperties, SceneParentReferencePair sceneParentReferencePair) in worldNPCRows)
                {
                    if (characterProperties == null) { continue; }
                    worldNPCLookup[characterProperties] = sceneParentReferencePair;
                }
                var updatedSaveData = new PartySaveData(partyCharacters, unlockedCharactersSet, worldNPCLookup);
                saveState = party.ManualGetStateFromData(updatedSaveData);
                RaiseSaveStateChanged();

                if (!redrawCharacterEntityView) { return; }
                ReconcileEntityView(partyCharacters);
            }
        }
        
        private static void DrawPartyCharacterList(VisualElement container, List<CharacterProperties> partyCharacters, Action<bool> pushSaveState)
        {
            container.Clear();

            for (int i = 0; i < partyCharacters.Count; i++)
            {
                int rowIndex = i;

                var row = new VisualElement { style = { flexDirection = FlexDirection.Row } };
                container.Add(row);

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
                    
                    partyCharacters[rowIndex] = changeEvent.newValue as CharacterProperties;
                    pushSaveState?.Invoke(true);
                });

                removeButton.RegisterCallback<ClickEvent>(_ =>
                {
                    partyCharacters.RemoveAt(rowIndex);
                    pushSaveState?.Invoke(true);
                    DrawPartyCharacterList(container, partyCharacters, pushSaveState);
                });
            }
        }
        
        private static void DrawUnlockedCharacterList(VisualElement container, List<CharacterProperties> unlockedCharacters, Action<bool> pushSaveState)
        {
            container.Clear();

            for (int i = 0; i < unlockedCharacters.Count; i++)
            {
                int rowIndex = i;

                var row = new VisualElement { style = { flexDirection = FlexDirection.Row } };
                container.Add(row);

                var characterField = new ObjectField { objectType = typeof(CharacterProperties), value = unlockedCharacters[rowIndex], style = { flexGrow = 1 } };
                row.Add(characterField);

                var removeButton = new Button { text = "- Remove", style = { width = smallButtonWidth }  };
                row.Add(removeButton);

                characterField.RegisterValueChangedCallback(changeEvent =>
                {
                    var newCharacterProperties = changeEvent.newValue as CharacterProperties;
                    if (newCharacterProperties != null && newCharacterProperties.GetCharacterPrefab() == null)
                    {
                        Debug.LogWarning("Invalid Character:  Select entry with a character prefab.");
                        characterField.SetValueWithoutNotify(unlockedCharacters[rowIndex]);
                        return;
                    }

                    bool isDuplicate = newCharacterProperties != null && unlockedCharacters
                        .Where((_, index) => index != rowIndex)
                        .Any(characterProperties => characterProperties == newCharacterProperties);
                    if (isDuplicate)
                    {
                        characterField.SetValueWithoutNotify(unlockedCharacters[rowIndex]);
                        return;
                    }

                    unlockedCharacters[rowIndex] = newCharacterProperties;
                    pushSaveState?.Invoke(false);
                });

                removeButton.RegisterCallback<ClickEvent>(_ =>
                {
                    unlockedCharacters.RemoveAt(rowIndex);
                    pushSaveState?.Invoke(false);
                    DrawUnlockedCharacterList(container, unlockedCharacters, pushSaveState);
                });
            }
        }
        
        private static void DrawWorldNPCList(VisualElement container, List<(CharacterProperties characterProperties, SceneParentReferencePair sceneParentReferencePair)> worldNPCRows, Action<bool> pushSaveState)
        {
            container.Clear();

            for (int i = 0; i < worldNPCRows.Count; i++)
            {
                int rowIndex = i;
                (CharacterProperties characterProperties, SceneParentReferencePair sceneParentReferencePair) = worldNPCRows[rowIndex];

                var column = new VisualElement { style = { flexDirection = FlexDirection.Column } };
                container.Add(column);

                var characterRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };
                var characterField = new ObjectField { objectType = typeof(CharacterProperties), value = characterProperties, style = { flexGrow = 1 } };
                characterRow.Add(characterField);
                var removeButton = new Button { text = "- Remove", style = { width = smallButtonWidth }  };
                characterRow.Add(removeButton);
                column.Add(characterRow);
                
                var sceneField = new TextField { label ="Scene Name", value = sceneParentReferencePair.sceneName, isDelayed = true, style = { flexGrow = 1 }};
                column.Add(sceneField);

                var parentField = new TextField { label = "Parent Object Name", value = sceneParentReferencePair.parentName, isDelayed = true, style = { flexGrow = 1 } };
                column.Add(parentField);

                characterField.RegisterValueChangedCallback(changeEvent =>
                {
                    var newCharacterProperties = changeEvent.newValue as CharacterProperties;

                    bool isDuplicateKey = newCharacterProperties != null && worldNPCRows
                        .Where((_, index) => index != rowIndex)
                        .Any(entry => entry.characterProperties == newCharacterProperties);
                    if (isDuplicateKey)
                    {
                        characterField.SetValueWithoutNotify(worldNPCRows[rowIndex].characterProperties);
                        return;
                    }

                    worldNPCRows[rowIndex] = (newCharacterProperties, worldNPCRows[rowIndex].sceneParentReferencePair);
                    pushSaveState?.Invoke(false);
                });

                sceneField.RegisterValueChangedCallback(changeEvent =>
                {
                    SceneParentReferencePair updatedPair = worldNPCRows[rowIndex].sceneParentReferencePair;
                    updatedPair.sceneName = changeEvent.newValue;
                    worldNPCRows[rowIndex] = (worldNPCRows[rowIndex].characterProperties, updatedPair);
                    pushSaveState?.Invoke(false);
                });

                parentField.RegisterValueChangedCallback(changeEvent =>
                {
                    SceneParentReferencePair updatedPair = worldNPCRows[rowIndex].sceneParentReferencePair;
                    updatedPair.parentName = changeEvent.newValue;
                    worldNPCRows[rowIndex] = (worldNPCRows[rowIndex].characterProperties, updatedPair);
                    pushSaveState?.Invoke(false);
                });

                removeButton.RegisterCallback<ClickEvent>(_ =>
                {
                    worldNPCRows.RemoveAt(rowIndex);
                    pushSaveState?.Invoke(false);
                    DrawWorldNPCList(container, worldNPCRows, pushSaveState);
                });
            }
        }
    }
}
