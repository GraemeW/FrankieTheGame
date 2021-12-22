using Frankie.Combat;
using Frankie.Saving;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Stats
{
    public class InactiveParty : MonoBehaviour, ISaveable
    {
        // State
        Dictionary<CharacterProperties, object> inactiveCharacterSaveStates = new Dictionary<CharacterProperties, object>();

        #region PublicMethods
        public void CaptureCharacterState(CombatParticipant combatParticipant)
        {
            if (combatParticipant == null) { return; }

            SaveableEntity saveableEntity = combatParticipant.GetComponent<SaveableEntity>();
            if (saveableEntity == null) { return; }

            CharacterProperties characterProperties = combatParticipant.GetBaseStats().GetCharacterProperties();
            inactiveCharacterSaveStates[characterProperties] = saveableEntity.CaptureState();
        }

        public void RestoreCharacterState(ref CombatParticipant combatParticipant)
        {
            if (combatParticipant == null) { return; }

            CharacterProperties characterProperties = combatParticipant.GetBaseStats().GetCharacterProperties();
            if (!inactiveCharacterSaveStates.ContainsKey(characterProperties)) { return; }

            SaveableEntity saveableEntity = combatParticipant.GetComponent<SaveableEntity>();
            if (saveableEntity == null) { return; }

            saveableEntity.RestoreState(inactiveCharacterSaveStates[characterProperties], LoadPriority.ObjectProperty);
        }

        public void RemoveFromInactiveStorage(CombatParticipant combatParticipant)
        {
            if (combatParticipant == null) { return; }

            CharacterProperties characterProperties = combatParticipant.GetBaseStats().GetCharacterProperties();
            RemoveFromInactiveStorage(characterProperties);
        }

        public void RemoveFromInactiveStorage(CharacterProperties characterProperties)
        {
            inactiveCharacterSaveStates.Remove(characterProperties);
        }

        #endregion

        #region SaveSystem
        public LoadPriority GetLoadPriority()
        {
            return LoadPriority.ObjectProperty;
        }

        public SaveState CaptureState()
        {
            Dictionary<string, object> serializableInactiveCharacterSaveStates = new Dictionary<string, object>();
            foreach (KeyValuePair<CharacterProperties, object> keyValuePair in inactiveCharacterSaveStates)
            {
                string characterName = keyValuePair.Key.name;
                serializableInactiveCharacterSaveStates[characterName] = keyValuePair.Value;
            }

            SaveState saveState = new SaveState(GetLoadPriority(), serializableInactiveCharacterSaveStates);
            return saveState;
        }

        public void RestoreState(SaveState state)
        {
            Dictionary<string, object> serializableInactiveCharacterSaveStates = state.GetState() as Dictionary<string, object>;
            if (serializableInactiveCharacterSaveStates == null) { return; }

            inactiveCharacterSaveStates.Clear();
            foreach (KeyValuePair<string, object> keyValuePair in serializableInactiveCharacterSaveStates)
            {
                string characterName = keyValuePair.Key;
                CharacterProperties characterProperties = CharacterProperties.GetCharacterPropertiesFromName(characterName);
                if (characterProperties == null) { continue; }

                inactiveCharacterSaveStates[characterProperties] = keyValuePair.Value;
            }
        }
        #endregion
    }
}