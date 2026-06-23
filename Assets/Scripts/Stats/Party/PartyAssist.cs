using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Frankie.Saving;

namespace Frankie.Stats
{
    [RequireComponent(typeof(Party))]
    public class PartyAssist : PartyBehaviour, ISaveable<List<CharacterProperties>>
    {
        // Cached References
        private Party party;

        // Events
        public event Action partyAssistUpdated;

        #region UnityMethods
        protected override void Awake()
        {
            base.Awake();
            party = GetComponent<Party>();
        }
        #endregion

        #region AbstractImplementations
        protected override int GetInitialPartyOffset() => party.GetLastMemberOffsetIndex() + partyOffset;
        protected override bool ShouldSkipFirstEntryOffset() => false;

        protected override bool AddToParty(BaseStats characterBaseStats)
        {
            if (members.Count >= partyLimit) { return false; }
            if (characterBaseStats == null) { return false; } // Failsafe
            if (HasMember(characterBaseStats)) { return false; } // Verify no dupe characters to party

            members.Add(characterBaseStats);
            RefreshAnimatorLookup();

            partyAssistUpdated?.Invoke();
            return true;
        }

        public override bool AddToParty(CharacterNPCSwapper characterNPCSwapper)
        {
            // For direct interaction with world NPCs -> characters
            if (members.Count >= partyLimit) { return false; }
            if (characterNPCSwapper == null) { return false; } // Failsafe

            CharacterNPCSwapper partyCharacter = characterNPCSwapper.SwapToCharacter(container);
            if (partyCharacter == null) { return false; }
            
            party.UpdateWorldLookup(false, partyCharacter);
            Destroy(characterNPCSwapper.gameObject);
            return AddToParty(partyCharacter.GetBaseStats());
        }

        public override bool AddToParty(CharacterProperties characterProperties)
        {
            // For instantiation through other means (i.e. no character exists on screen)
            if (members.Count >= partyLimit) { return false; }
            if (characterProperties == null) { return false; } // Failsafe

            GameObject characterObject = CharacterNPCSwapper.SpawnCharacter(characterProperties, container);
            if (characterObject == null) { return false; }

            var character = characterObject.GetComponent<BaseStats>();
            return AddToParty(character);
        }

        public override bool RemoveFromParty(BaseStats character)
        {
            if (character == null) { return false; } // Failsafe

            members.Remove(character);
            characterSpriteLinkLookup.Remove(character);

            Destroy(character.gameObject);
            partyAssistUpdated?.Invoke();

            return true;
        }

        public override bool RemoveFromParty(CharacterProperties characterProperties)
        {
            if (characterProperties == null) { return false; } // Failsafe

            BaseStats member = GetMember(characterProperties);
            return member != null && RemoveFromParty(member);
        }

        public override bool RemoveFromParty(BaseStats character, Transform worldTransform)
        {
            if (character == null) { return false; } // Failsafe

            // Instantiates an NPC at defined location
            var partyCharacter = character.GetComponent<CharacterNPCSwapper>();
            if (partyCharacter == null) { return false; }

            CharacterNPCSwapper worldNPC = partyCharacter.SwapToNPC(worldTransform);
            if (worldNPC == null) { return false; }
            party.UpdateWorldLookup(true, worldNPC);

            return RemoveFromParty(character);
        }
        #endregion
        
        #region SaveInterface
        public LoadPriority GetLoadPriority() => LoadPriority.ObjectInstantiation;

        public SaveState CaptureState()
        {
            members ??= new List<BaseStats>();
            List<CharacterProperties> partyCharacters = members.Select(character => character.GetCharacterProperties()).ToList();
            return ManualGetStateFromData(partyCharacters);
        }

        public void RestoreState(SaveState saveState)
        {
            List<CharacterProperties> partyCharacters = ManualGetDataFromState(saveState);
            RestorePartyMembers(partyCharacters);
        }
        
        public SaveState ManualGetStateFromData(List<CharacterProperties> data)
        {
            List<string> partyNames = data.Select(character => character.name).ToList();
            return new SaveState(GetLoadPriority(), partyNames);
        }

        public List<CharacterProperties> ManualGetDataFromState(SaveState saveState)
        {
            if (saveState.GetState(typeof(List<string>)) is not List<string> partyNames) { return new List<CharacterProperties>(); }
            return partyNames.Select(CharacterProperties.GetCharacterPropertiesFromName).Where(partyCharacter => partyCharacter != null).ToList();
        }
        
        private void RestorePartyMembers(List<CharacterProperties> partyCharacters)
        {
            if (partyCharacters == null) { return; }
            
            // Clear characters in existing party in scene
            foreach (BaseStats character in members) { Destroy(character.gameObject); }
            members.Clear();

            // Pull characters from save
            foreach (CharacterProperties partyCharacter in partyCharacters)
            {
                if (members.Count > partyLimit) { break; } // Failsafe

                GameObject characterObject = CharacterNPCSwapper.SpawnCharacter(partyCharacter, container);
                if (characterObject == null) { continue; }

                var character = characterObject.GetComponent<BaseStats>();
                if (character == null) { Destroy(characterObject); continue; }

                members.Add(character);

                if (members.Count > 1) { characterObject.GetComponent<Collider2D>().isTrigger = true; }
            }
            RefreshAnimatorLookup();
            partyAssistUpdated?.Invoke();
        }
        #endregion
    }
}
