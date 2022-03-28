using Frankie.Control;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Stats
{
    [RequireComponent(typeof(BaseStats))]
    public class CharacterNPCSwapper : MonoBehaviour
    {
        // Cached References

        BaseStats baseStats = null;
        Party party = null;

        private void Awake()
        {
            baseStats = GetComponent<BaseStats>();
            party = GameObject.FindGameObjectWithTag("Player")?.GetComponent<Party>();
        }

        private void Start()
        {
            DeleteNPCIfInParty();
        }

        private void DeleteNPCIfInParty()
        {
            if (party ==  null) { return; }

            CharacterProperties characterProperties = baseStats.GetCharacterProperties();
            if (characterProperties == null) { return; }

            BaseStats characterInParty = party.GetMember(characterProperties);
            if (characterInParty != null && characterInParty != this.baseStats)
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

        public BaseStats GetBaseStats() => baseStats;

        public CharacterNPCSwapper SwapToCharacter(Transform partyContainer)
        {
            string characterName = baseStats.GetCharacterProperties().GetCharacterNameID();
            GameObject character = SpawnCharacter(characterName, partyContainer);

            // Pass stats back/forth NPC -> Character
            BaseStats characterBaseStats = character.GetComponent<BaseStats>();
            characterBaseStats.SetActiveStatSheet(baseStats.GetActiveStatSheet());
            characterBaseStats.OverrideLevel(baseStats.GetLevel());

            CharacterNPCSwapper partyCharacter = character.GetComponent<CharacterNPCSwapper>();
            return partyCharacter;
        }

        public CharacterNPCSwapper SwapToNPC(Transform worldContainer)
        {
            string characterName = baseStats.GetCharacterProperties().GetCharacterNameID();
            GameObject characterNPC = SpawnNPC(characterName, worldContainer);

            // Pass stats back/forth Character -> NPC
            BaseStats characterNPCBaseStats = characterNPC.GetComponent<BaseStats>();
            characterNPCBaseStats.SetActiveStatSheet(baseStats.GetActiveStatSheet());
            characterNPCBaseStats.OverrideLevel(baseStats.GetLevel());

            CharacterNPCSwapper worldNPC = characterNPC.GetComponent<CharacterNPCSwapper>();
            return worldNPC;
        }

        public void JoinParty(PlayerStateMachine playerStateHandler) // Called via Unity Events
        {
            Party party = playerStateHandler.GetParty();
            party.AddToParty(this);
        }
    }

}