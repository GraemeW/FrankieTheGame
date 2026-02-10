using UnityEngine;
using Frankie.Control;
using Frankie.Core;
using Frankie.Utils;

namespace Frankie.Stats
{
    [RequireComponent(typeof(BaseStats))]
    public class CharacterNPCSwapper : MonoBehaviour
    {
        // Cached References
        private BaseStats baseStats;
        private ReInitLazyValue<Party> party;

        #region StaticMethods
        private static Party SetupPartyReference()
        {
            GameObject playerObject = Player.FindPlayerObject();
            return playerObject == null ? null : playerObject.GetComponent<Party>();
        }
        
        public static GameObject SpawnCharacter(string characterName, Transform partyTransform)
        {
            if (characterName == null || partyTransform == null) { return null; }

            CharacterProperties characterProperties = CharacterProperties.GetCharacterPropertiesFromName(characterName);
            if (characterProperties == null) { return null; }
            
            GameObject characterPrefab = characterProperties.characterPrefab;
            if (characterPrefab == null) { return null; }
            
            GameObject character = Instantiate(characterPrefab, partyTransform);
            return character;
        }

        public static GameObject SpawnNPC(string characterName, Transform worldContainer)
        {
            if (characterName == null || worldContainer == null) { return null; }

            CharacterProperties characterProperties = CharacterProperties.GetCharacterPropertiesFromName(characterName);
            if (characterProperties == null) { return null; }
            
            GameObject characterNPCPrefab = characterProperties.characterNPCPrefab;
            if (characterNPCPrefab == null) { return null; }
            
            GameObject characterNPC = Instantiate(characterNPCPrefab, worldContainer);
            return characterNPC;
        }
        #endregion
        
        #region UnityMethods
        private void Awake()
        {
            baseStats = GetComponent<BaseStats>();
            party = new ReInitLazyValue<Party>(SetupPartyReference);
        }

        private void Start()
        {
            party.ForceInit();
            DeleteNPCIfInParty();
        }
        #endregion

        #region PrivateMethods
        private void DeleteNPCIfInParty()
        {
            if (party == null) { return; }

            CharacterProperties characterProperties = baseStats.GetCharacterProperties();
            if (characterProperties == null) { return; }

            BaseStats characterInParty = party.value.GetMember(characterProperties);
            if (characterInParty != null && characterInParty != baseStats)
            {
                Destroy(gameObject);
            }
        }
        #endregion

        #region
        public BaseStats GetBaseStats() => baseStats;

        public CharacterNPCSwapper SwapToCharacter(Transform partyContainer)
        {
            string characterName = baseStats.GetCharacterProperties().GetCharacterNameID();
            GameObject character = SpawnCharacter(characterName, partyContainer);

            // Pass stats back/forth NPC -> Character
            var characterBaseStats = character.GetComponent<BaseStats>();
            characterBaseStats.SetActiveStatSheet(baseStats.GetActiveStatSheet());
            characterBaseStats.OverrideLevel(baseStats.GetLevel());

            var partyCharacter = character.GetComponent<CharacterNPCSwapper>();
            return partyCharacter;
        }

        public CharacterNPCSwapper SwapToNPC(Transform worldContainer)
        {
            string characterName = baseStats.GetCharacterProperties().GetCharacterNameID();
            GameObject characterNPC = SpawnNPC(characterName, worldContainer);

            // Pass stats back/forth Character -> NPC
            var characterNPCBaseStats = characterNPC.GetComponent<BaseStats>();
            characterNPCBaseStats.SetActiveStatSheet(baseStats.GetActiveStatSheet());
            characterNPCBaseStats.OverrideLevel(baseStats.GetLevel());

            var worldNPC = characterNPC.GetComponent<CharacterNPCSwapper>();
            return worldNPC;
        }

        public void JoinParty(PlayerStateMachine playerStateMachine) // Called via Unity Events
        {
            if (playerStateMachine.TryGetComponent(out Party passParty))
            {
                passParty.AddToParty(this);
            }
        }

        public void JoinPartyAssist(PlayerStateMachine playerStateMachine) // Called via Unity Events
        {
            if (playerStateMachine.TryGetComponent(out PartyAssist partyAssist))
            {
                partyAssist.AddToParty(this);
            }
        }
        #endregion
    }
}
