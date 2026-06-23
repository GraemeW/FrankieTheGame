using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using UnityEngine;
using Frankie.Saving;

namespace Frankie.Stats
{
    public class InactiveParty : MonoBehaviour, ISaveable<Dictionary<CharacterProperties, JToken>>
    {
        // State
        private readonly Dictionary<CharacterProperties, JToken> inactiveCharacterSaveStates = new();

        #region PublicMethods
        public void CaptureCharacterState(BaseStats character)
        {
            if (character == null) { return; }

            var saveableEntity = character.GetComponent<SaveableEntity>();
            if (saveableEntity == null) { return; }

            CharacterProperties characterProperties = character.GetCharacterProperties();
            if (characterProperties == null) { return; }
            inactiveCharacterSaveStates[characterProperties] = saveableEntity.CaptureState(null);
        }

        public void RestoreCharacterState(ref BaseStats character)
        {
            if (character == null) { return; }

            CharacterProperties characterProperties = character.GetCharacterProperties();
            if (characterProperties == null) { return; }
            if (!inactiveCharacterSaveStates.ContainsKey(characterProperties)) { return; }

            SaveableEntity saveableEntity = character.GetComponent<SaveableEntity>();
            if (saveableEntity == null) { return; }

            saveableEntity.RestoreState(inactiveCharacterSaveStates[characterProperties], LoadPriority.ObjectProperty);
        }

        public void RemoveFromInactiveStorage(BaseStats character)
        {
            if (character == null) { return; }

            CharacterProperties characterProperties = character.GetCharacterProperties();
            RemoveFromInactiveStorage(characterProperties);
        }

        public void RemoveFromInactiveStorage(CharacterProperties characterProperties)
        {
            if (characterProperties == null) { return; }
            inactiveCharacterSaveStates.Remove(characterProperties);
        }
        #endregion

        #region SaveSystem
        public bool IsCorePlayerState() => true;
        public LoadPriority GetLoadPriority() => LoadPriority.ObjectProperty;
        public SaveState CaptureState() => ManualGetStateFromData(inactiveCharacterSaveStates);

        public void RestoreState(SaveState saveState)
        {
            if (saveState.GetState(typeof(Dictionary<string, JToken>)) is not Dictionary<string, JToken>) { return; }
            
            inactiveCharacterSaveStates.Clear();
            foreach (KeyValuePair<CharacterProperties, JToken> characterPropertiesDataPair in ManualGetDataFromState(saveState))
            {
                inactiveCharacterSaveStates[characterPropertiesDataPair.Key] = characterPropertiesDataPair.Value;
            }
        }
        
        public SaveState ManualGetStateFromData(Dictionary<CharacterProperties, JToken> data)
        {
            data ??= new Dictionary<CharacterProperties, JToken>();
            
            Dictionary<string, JToken> inactiveCharacterSaveStateRecords = new Dictionary<string, JToken>();
            foreach (KeyValuePair<CharacterProperties, JToken> keyValuePair in data)
            {
                inactiveCharacterSaveStateRecords[keyValuePair.Key.GetCharacterID()] = keyValuePair.Value;
            }
            return new SaveState(GetLoadPriority(), inactiveCharacterSaveStateRecords);
        }

        public Dictionary<CharacterProperties, JToken> ManualGetDataFromState(SaveState saveState)
        {
            if (saveState.GetState(typeof(Dictionary<string, JToken>)) is not Dictionary<string, JToken> inactiveCharacterSaveStateRecords) { return new Dictionary<CharacterProperties, JToken>(); }
            
            var data = new Dictionary<CharacterProperties, JToken>();
            foreach (KeyValuePair<string, JToken> keyValuePair in inactiveCharacterSaveStateRecords)
            {
                string characterName = keyValuePair.Key;
                if (string.IsNullOrWhiteSpace(characterName)) { continue; }
                CharacterProperties characterProperties = CharacterProperties.GetCharacterPropertiesFromName(characterName);
                if (characterProperties == null) { continue; }

                data[characterProperties] = keyValuePair.Value;
            }
            return data;
        }
        #endregion
    }
}
