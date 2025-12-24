using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using UnityEngine;
using Frankie.Saving;

namespace Frankie.Stats
{
    public class InactiveParty : MonoBehaviour, ISaveable
    {
        // State
        private readonly Dictionary<string, JToken> inactiveCharacterSaveStates = new();

        #region PublicMethods
        public void CaptureCharacterState(BaseStats character)
        {
            if (character == null) { return; }

            var saveableEntity = character.GetComponent<SaveableEntity>();
            if (saveableEntity == null) { return; }

            CharacterProperties characterProperties = character.GetCharacterProperties();
            if (characterProperties == null) { return; }
            inactiveCharacterSaveStates[characterProperties.GetCharacterNameID()] = saveableEntity.CaptureState(null);
        }

        public void RestoreCharacterState(ref BaseStats character)
        {
            if (character == null) { return; }

            CharacterProperties characterProperties = character.GetCharacterProperties();
            if (characterProperties == null) { return; }
            if (!inactiveCharacterSaveStates.ContainsKey(characterProperties.GetCharacterNameID())) { return; }

            SaveableEntity saveableEntity = character.GetComponent<SaveableEntity>();
            if (saveableEntity == null) { return; }

            saveableEntity.RestoreState(inactiveCharacterSaveStates[characterProperties.GetCharacterNameID()], LoadPriority.ObjectProperty);
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
            inactiveCharacterSaveStates.Remove(characterProperties.GetCharacterNameID());
        }
        #endregion

        #region SaveSystem
        public bool IsCorePlayerState() => true;
        public LoadPriority GetLoadPriority() => LoadPriority.ObjectProperty;
        public SaveState CaptureState()
        {
            var saveState = new SaveState(GetLoadPriority(), inactiveCharacterSaveStates);
            return saveState;
        }

        public void RestoreState(SaveState state)
        {
            if (state.GetState(typeof(Dictionary<string, JToken>)) is not Dictionary<string, JToken> inactiveCharacterSaveStateRecords) { return; }
            
            inactiveCharacterSaveStates.Clear();
            foreach (KeyValuePair<string, JToken> keyValuePair in inactiveCharacterSaveStateRecords)
            {
                string characterName = keyValuePair.Key;
                if (string.IsNullOrWhiteSpace(characterName)) { continue; }

                inactiveCharacterSaveStates[characterName] = keyValuePair.Value;
            }
        }
        #endregion
    }
}
