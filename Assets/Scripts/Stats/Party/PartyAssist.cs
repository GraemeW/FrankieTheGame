using Frankie.Control;
using Frankie.Saving;
using Frankie.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Stats
{
    [RequireComponent(typeof(Party))]
    public class PartyAssist : PartyBehaviour, ISaveable
    {
        // Cached References
        Party party = null;

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

        protected override bool AddToParty(BaseStats character)
        {
            if (members.Count >= partyLimit) { return false; }
            if (character == null) { return false; } // Failsafe
            if (HasMember(character)) { return false; } // Verify no dupe characters to party

            members.Add(character);
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
            party.UpdateWorldLookup(false, partyCharacter);
            Destroy(characterNPCSwapper.gameObject);

            return AddToParty(partyCharacter.GetBaseStats());
        }

        public override bool AddToParty(CharacterProperties characterProperties)
        {
            // For instantiation through other means (i.e. no character exists on screen)
            if (members.Count >= partyLimit) { return false; }
            if (characterProperties == null) { return false; } // Failsafe

            GameObject characterObject = CharacterNPCSwapper.SpawnCharacter(characterProperties.name, container);

            BaseStats character = characterObject.GetComponent<BaseStats>();
            return AddToParty(character);
        }

        public override bool RemoveFromParty(BaseStats character)
        {
            if (character == null) { return false; } // Failsafe

            members.Remove(character);
            animatorLookup.Remove(character);

            Destroy(character.gameObject);
            partyAssistUpdated?.Invoke();

            return true;
        }

        public override bool RemoveFromParty(CharacterProperties characterProperties)
        {
            if (characterProperties == null) { return false; } // Failsafe

            BaseStats member = GetMember(characterProperties);
            return member != null ? RemoveFromParty(member) : false;
        }

        public override bool RemoveFromParty(BaseStats character, Transform worldTransform)
        {
            if (character == null) { return false; } // Failsafe

            // Instantiates an NPC at defined location
            CharacterNPCSwapper partyCharacter = character.GetComponent<CharacterNPCSwapper>();
            if (partyCharacter == null) { return false; }

            CharacterNPCSwapper worldNPC = partyCharacter.SwapToNPC(worldTransform);
            party.UpdateWorldLookup(true, worldNPC);

            return RemoveFromParty(character);
        }

        public LoadPriority GetLoadPriority()
        {
            return LoadPriority.ObjectInstantiation;
        }

        public SaveState CaptureState()
        {
            List<string> currentPartyStrings = new List<string>();
            foreach (BaseStats character in members)
            {
                currentPartyStrings.Add(character.GetCharacterProperties().name);
            }

            SaveState saveState = new SaveState(GetLoadPriority(), currentPartyStrings);
            return saveState;
        }

        public void RestoreState(SaveState saveState)
        {
            List<string> addPartyStrings = saveState.GetState(typeof(List<string>)) as List<string>;
            if (addPartyStrings == null || addPartyStrings.Count == 0) { return; }

            // Clear characters in existing party in scene
            foreach (BaseStats character in members)
            {
                Destroy(character.gameObject);
            }
            members.Clear();

            // Pull characters from save
            foreach (string characterName in addPartyStrings)
            {
                if (members.Count > partyLimit) { break; } // Failsafe

                GameObject characterObject = CharacterNPCSwapper.SpawnCharacter(characterName, container);
                if (characterObject == null) { return; }

                BaseStats character = characterObject.GetComponent<BaseStats>();
                if (character == null) { Destroy(characterObject); return; }

                members.Add(character);

                if (members.Count > 1) { characterObject.GetComponent<Collider2D>().isTrigger = true; }
            }
            RefreshAnimatorLookup();

            partyAssistUpdated?.Invoke();
        }
        #endregion
    }
}