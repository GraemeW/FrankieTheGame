using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using LowDefMustard.Saving;
using Frankie.Saving;

namespace Frankie.Stats
{
    public class InactiveParty : MonoBehaviour, ISaveable<HashSet<CharacterProperties>>
    {
        // State ~ keyed by character ID:  builds hold separate built-in + Addressables copies of each CharacterProperties
        private readonly HashSet<string> inactiveCharacterIDs = new();

        #region PublicMethods
        public void CaptureCharacterState(BaseStats character)
        {
            if (character == null) { return; }

            var saveableEntity = character.GetComponent<SaveableEntity>();
            if (saveableEntity == null) { return; }

            CharacterProperties characterProperties = character.GetCharacterProperties();
            if (characterProperties == null) { return; }
            
            SaveFileManager.AppendToSession(saveableEntity);
            inactiveCharacterIDs.Add(characterProperties.GetCharacterID());
        }

        public void RestoreCharacterState(BaseStats character)
        {
            if (character == null) { return; }

            CharacterProperties characterProperties = character.GetCharacterProperties();
            if (characterProperties == null) { return; }
            if (!inactiveCharacterIDs.Contains(characterProperties.GetCharacterID())) { return; }

            SaveableEntity saveableEntity = character.GetComponent<SaveableEntity>();
            if (saveableEntity == null) { return; }
            
            SaveFileManager.RestorePropertiesFromSession(saveableEntity);
            inactiveCharacterIDs.Remove(characterProperties.GetCharacterID());
        }
        #endregion

        #region SaveSystem
        public bool IsCorePlayerState() => true;
        public LoadPriority GetLoadPriority() => LoadPriority.ObjectProperty;
        public SaveState CaptureState() => new(GetLoadPriority(), new HashSet<string>(inactiveCharacterIDs));

        public void RestoreState(SaveState saveState)
        {
            if (!TryManualGetDataFromState(saveState, out HashSet<CharacterProperties> data)) { return; }
            foreach (CharacterProperties characterProperties in data)
            {
                inactiveCharacterIDs.Add(characterProperties.GetCharacterID());
            }
        }
        
        public SaveState ManualGetStateFromData(HashSet<CharacterProperties> data)
        {
            data ??= new HashSet<CharacterProperties>();
            HashSet<string> partyNames = data.Select(character => character != null ? character.GetCharacterID() : string.Empty).ToHashSet();
            return new SaveState(GetLoadPriority(), partyNames);
        }

        public bool TryManualGetDataFromState(SaveState saveState, out HashSet<CharacterProperties> data)
        {
            data = new HashSet<CharacterProperties>();
            // Default Pass Back
            if (saveState == null || !saveState.TryGetState(out List<string> partyNames)) { return true; }
            
            // Save Found Pass Back
            data = partyNames.Select(CharacterProperties.GetCharacterPropertiesFromName).Where(partyCharacter => partyCharacter != null).ToHashSet();
            return true;
        }
        #endregion
    }
}
