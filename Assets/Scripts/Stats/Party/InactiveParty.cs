using Frankie.Saving;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Stats
{
    public class InactiveParty : MonoBehaviour, ISaveable
    {
        // State
        Dictionary<string, JToken> inactiveCharacterSaveStates = new Dictionary<string, JToken>();

        #region PublicMethods
        public void CaptureCharacterState(BaseStats character)
        {
            if (character == null) { return; }

            SaveableEntity saveableEntity = character.GetComponent<SaveableEntity>();
            if (saveableEntity == null) { return; }

            CharacterProperties characterProperties = character.GetCharacterProperties();
            if (characterProperties == null) { return; }
            inactiveCharacterSaveStates[characterProperties.GetCharacterNameID()] = saveableEntity.CaptureState();
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
        public LoadPriority GetLoadPriority()
        {
            return LoadPriority.ObjectProperty;
        }

        public SaveState CaptureState()
        {
            SaveState saveState = new SaveState(GetLoadPriority(), inactiveCharacterSaveStates);
            return saveState;
        }

        public void RestoreState(SaveState state)
        {
            Dictionary<string, JToken> inactiveCharacterSaveStateRecords = state.GetState(typeof(Dictionary<string, JToken>)) as Dictionary<string, JToken>;
            if (inactiveCharacterSaveStateRecords == null) { return; }

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