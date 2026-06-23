using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine.UIElements;
using Frankie.Stats;
using UnityEngine;

namespace Frankie.Saving.Editor
{
    public class InactivePartySubCard : SaveableSubCardData
    {
        public InactivePartySubCard(ISaveableBase saveable, SaveState saveState)
        {
            this.saveable = saveable;
            this.saveState = saveState;
        }

        public override void AddEditableFieldsToSubCardView(Box subCardView)
        {
            if (saveable is not InactiveParty inactiveParty) { return; }
            
            Dictionary<CharacterProperties, JToken> inactivePartyData = inactiveParty.ManualGetDataFromState(saveState);
            if (inactivePartyData == null)
            {
                subCardView.Add(new Label("No PartyAssist save data found"));
                return;
            }
            
            List<(CharacterProperties characterProperties, SaveableEntityCardData entityCardData)> inactivePartyEntries = BuildInactivePartySaveableEntityData(inactivePartyData);
            if (inactivePartyEntries.Count == 0)
            {
                subCardView.Add(new Label("No inactive party members"));
                return;
            }

            foreach ((CharacterProperties _, SaveableEntityCardData inactiveCharacterEntry) in inactivePartyEntries)
            {
                var entityBox = new Box();
                subCardView.Add(entityBox);

                entityBox.Add(new Label(inactiveCharacterEntry.entityName));
                foreach (SaveableSubCardData inactiveCharacterSubCardEntry in inactiveCharacterEntry.subCards.Values)
                {
                    var nestedSubCardBox = new Box { style = { marginLeft = 8 } };
                    entityBox.Add(nestedSubCardBox);

                    nestedSubCardBox.Add(new Label(inactiveCharacterSubCardEntry.saveable.GetType().ToString()));
                    inactiveCharacterSubCardEntry.AddEditableFieldsToSubCardView(nestedSubCardBox);

                    inactiveCharacterSubCardEntry.SubscribeToStateChangedEvent(false, OnNestedSaveStateChanged);
                    inactiveCharacterSubCardEntry.SubscribeToStateChangedEvent(true, OnNestedSaveStateChanged);
                    continue;

                    
                    void OnNestedSaveStateChanged(string typeString, SaveState updatedSaveState)
                    {
                        inactiveCharacterEntry.UpdateSaveableEntry(typeString, updatedSaveState);
                        PushSaveState();
                    }
                }
            }
            return;

            // Local Functions
            void PushSaveState()
            {
                var updatedInactivePartyData = new Dictionary<CharacterProperties, JToken>();
                foreach ((CharacterProperties characterProperties, SaveableEntityCardData entityCardData) in inactivePartyEntries)
                {
                    updatedInactivePartyData[characterProperties] = entityCardData.saveableEntityStateDict;
                }
                saveState = inactiveParty.ManualGetStateFromData(updatedInactivePartyData);
                RaiseSaveStateChanged();
            }
        }

        private List<(CharacterProperties characterProperties, SaveableEntityCardData entityCardData)> BuildInactivePartySaveableEntityData(Dictionary<CharacterProperties, JToken> inactivePartyData)
        {
            var inactivePartyEntries = new List<(CharacterProperties characterProperties, SaveableEntityCardData entityCardData)>();
            foreach (KeyValuePair<CharacterProperties, JToken> characterPropertiesStateData in inactivePartyData)
            {
                GameObject inactiveCharacterPrefab = characterPropertiesStateData.Key.GetCharacterPrefab();
                if (inactiveCharacterPrefab == null) { continue; }
                var inactiveSaveableEntity = inactiveCharacterPrefab.GetComponent<SaveableEntity>();
                if (inactiveSaveableEntity == null) { continue; }
                
                if (!SaveableEntity.TryGetStateDictionary(characterPropertiesStateData.Value, out JObject saveableEntityStateDict)) { continue; }

                SaveableEntityCardData inactiveCharacterSaveableEntityData = new SaveableEntityCardData(inactiveSaveableEntity, saveableEntityStateDict);
                inactiveCharacterSaveableEntityData.SelfReferenceInSubCards();
                inactivePartyEntries.Add((characterPropertiesStateData.Key, inactiveCharacterSaveableEntityData));
            }
            return inactivePartyEntries;
        }
    }
}
