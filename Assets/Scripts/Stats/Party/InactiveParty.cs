using System.Collections.Generic;
using System.Linq;
using Frankie.Core;
using UnityEngine;
using Frankie.Saving;

namespace Frankie.Stats
{
    public class InactiveParty : MonoBehaviour, ISaveable<HashSet<CharacterProperties>>
    {
        // State
        private readonly HashSet<CharacterProperties> inactiveCharacters = new();

        #region PublicMethods
        public void CaptureCharacterState(BaseStats character)
        {
            if (character == null) { return; }

            var saveableEntity = character.GetComponent<SaveableEntity>();
            if (saveableEntity == null) { return; }

            CharacterProperties characterProperties = character.GetCharacterProperties();
            if (characterProperties == null) { return; }
            
            SavingWrapper.AppendToSession(saveableEntity);
            inactiveCharacters.Add(characterProperties);
        }

        public void RestoreCharacterState(BaseStats character)
        {
            if (character == null) { return; }

            CharacterProperties characterProperties = character.GetCharacterProperties();
            if (characterProperties == null) { return; }
            if (!inactiveCharacters.Contains(characterProperties)) { return; }

            SaveableEntity saveableEntity = character.GetComponent<SaveableEntity>();
            if (saveableEntity == null) { return; }
            
            SavingWrapper.RestorePropertiesFromSession(saveableEntity);
            inactiveCharacters.Remove(characterProperties);
        }
        #endregion

        #region SaveSystem
        public bool IsCorePlayerState() => true;
        public LoadPriority GetLoadPriority() => LoadPriority.ObjectProperty;
        public SaveState CaptureState() => ManualGetStateFromData(inactiveCharacters);

        public void RestoreState(SaveState saveState)
        {
            foreach (CharacterProperties characterProperties in ManualGetDataFromState(saveState))
            {
                inactiveCharacters.Add(characterProperties);
            }
        }
        
        public SaveState ManualGetStateFromData(HashSet<CharacterProperties> data)
        {
            data ??= new HashSet<CharacterProperties>();
            HashSet<string> partyNames = data.Select(character => character != null ? character.GetCharacterID() : string.Empty).ToHashSet();
            return new SaveState(GetLoadPriority(), partyNames);
        }

        public HashSet<CharacterProperties> ManualGetDataFromState(SaveState saveState)
        {
            if (saveState.GetState(typeof(List<string>)) is not List<string> partyNames) { return new HashSet<CharacterProperties>(); }
            return partyNames.Select(CharacterProperties.GetCharacterPropertiesFromName).Where(partyCharacter => partyCharacter != null).ToHashSet();
        }
        #endregion
    }
}
