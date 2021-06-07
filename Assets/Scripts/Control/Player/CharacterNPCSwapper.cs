using Frankie.Combat;
using Frankie.Stats;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Control
{
    public class CharacterNPCSwapper : MonoBehaviour
    {
        // Tunables
        [SerializeField] GameObject characterPrefab = null;
        [SerializeField] GameObject characterNPCPrefab = null;
        [SerializeField] BaseStats baseStats = null;
        [SerializeField] CombatParticipant combatParticipant = null;

        public CombatParticipant GetCombatParticipant()
        {
            return combatParticipant;
        }

        public CharacterNPCSwapper SwapToCharacter(Transform partyContainer)
        {
            if (characterPrefab == null || characterNPCPrefab == null) { return null; }

            GameObject character = Instantiate(characterPrefab, partyContainer);
            BaseStats characterBaseStats = character.GetComponent<BaseStats>();
            characterBaseStats.SetActiveStatSheet(baseStats.GetActiveStatSheet());
            CharacterNPCSwapper partyCharacter = character.GetComponent<CharacterNPCSwapper>();
            return partyCharacter;
        }

        public CharacterNPCSwapper SwapToNPC(Transform worldTransform)
        {
            if (characterPrefab == null || characterNPCPrefab == null) { return null; }

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