using Frankie.Combat;
using Frankie.Stats;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Control
{
    public class CharacterNPCSwapper : MonoBehaviour
    {
        // Cached References
        BaseStats baseStats = null;
        CombatParticipant combatParticipant = null;

        private void Awake()
        {
            baseStats = GetComponent<BaseStats>();
            combatParticipant = GetComponent<CombatParticipant>();
        }

        public CombatParticipant GetCombatParticipant()
        {
            return combatParticipant;
        }

        public CharacterNPCSwapper SwapToCharacter(Transform partyContainer)
        {
            GameObject characterPrefab = baseStats.GetCharacterProperties().GetCharacterPrefab();
            if (characterPrefab == null) { return null; }

            GameObject character = Instantiate(characterPrefab, partyContainer);
            BaseStats characterBaseStats = character.GetComponent<BaseStats>();
            characterBaseStats.SetActiveStatSheet(baseStats.GetActiveStatSheet());
            CharacterNPCSwapper partyCharacter = character.GetComponent<CharacterNPCSwapper>();
            return partyCharacter;
        }

        public CharacterNPCSwapper SwapToNPC(Transform worldTransform)
        {
            GameObject characterNPCPrefab = baseStats.GetCharacterProperties().GetCharacterNPCPrefab();
            if (characterNPCPrefab == null) { return null; }

            GameObject character = Instantiate(characterNPCPrefab, worldTransform);
            BaseStats characterNPCBaseStats = character.GetComponent<BaseStats>();
            characterNPCBaseStats.SetActiveStatSheet(baseStats.GetActiveStatSheet());
            CharacterNPCSwapper worldNPC = character.GetComponent<CharacterNPCSwapper>();
            return worldNPC;
        }

        public void JoinParty(PlayerStateHandler playerStateHandler)
        {
            Party party = playerStateHandler.GetParty();
            party.AddToParty(GetCombatParticipant());

            transform.position = party.GetPartyLeader().transform.position;
        }
    }

}