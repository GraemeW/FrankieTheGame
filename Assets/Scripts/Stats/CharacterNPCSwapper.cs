using Frankie.Combat;
using Frankie.Control;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Stats
{
    [RequireComponent(typeof(BaseStats))]
    [RequireComponent(typeof(CombatParticipant))]
    public class CharacterNPCSwapper : MonoBehaviour
    {
        // Cached References

        BaseStats baseStats = null;
        CombatParticipant combatParticipant = null;
        Party party = null;

        private void Awake()
        {
            baseStats = GetComponent<BaseStats>();
            combatParticipant = GetComponent<CombatParticipant>();
            party = GameObject.FindGameObjectWithTag("Player")?.GetComponent<Party>();
        }

        private void Start()
        {
            DeleteNPCIfInParty();
        }

        private void DeleteNPCIfInParty()
        {
            string characterName = baseStats.GetCharacterProperties().name;
            CombatParticipant characterInParty = party.GetMember(characterName);

            if (characterInParty != null && characterInParty != combatParticipant)
            {
                Destroy(gameObject);
            }
        }

        public static GameObject SpawnCharacter(string characterName, Transform partyTransform)
        {
            if (characterName == null || partyTransform == null) { return null; }

            CharacterProperties characterProperties = CharacterProperties.GetCharacterPropertiesFromName(characterName);
            GameObject characterPrefab = characterProperties.GetCharacterPrefab();

            GameObject character = Instantiate(characterPrefab, partyTransform);
            return character;
        }

        public static GameObject SpawnNPC(string characterName, Transform worldContainer)
        {
            if (characterName == null || worldContainer == null) { return null; }

            CharacterProperties characterProperties = CharacterProperties.GetCharacterPropertiesFromName(characterName);
            GameObject characterNPCPrefab = characterProperties.GetCharacterNPCPrefab();

            GameObject characterNPC = Instantiate(characterNPCPrefab, worldContainer);
            return characterNPC;
        }

        public CombatParticipant GetCombatParticipant()
        {
            return combatParticipant;
        }

        public BaseStats GetBaseStats()
        {
            return baseStats;
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

        public CharacterNPCSwapper SwapToNPC(Transform worldContainer)
        {
            GameObject characterNPCPrefab = baseStats.GetCharacterProperties().GetCharacterNPCPrefab();
            if (characterNPCPrefab == null) { return null; }

            GameObject character = Instantiate(characterNPCPrefab, worldContainer);
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