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
        
        public static GameObject SpawnCharacter(CharacterProperties characterProperties, Transform partyTransform)
        {
            if (characterProperties == null || partyTransform == null) { return null; }
            
            GameObject characterPrefab = characterProperties.GetCharacterPrefab();
            if (characterPrefab == null) { return null; }
            
            GameObject character = Instantiate(characterPrefab, partyTransform);
            return character;
        }

        public static GameObject SpawnNPC(CharacterProperties characterProperties, Transform worldContainer)
        {
            if (characterProperties == null) { return null; }
            GameObject characterNPCPrefab = characterProperties.GetCharacterNPCPrefab();
            if (characterNPCPrefab == null) { return null; }
            
            GameObject characterNPC = worldContainer != null ? Instantiate(characterNPCPrefab, worldContainer) :  Instantiate(characterNPCPrefab);
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
            party ??= new ReInitLazyValue<Party>(SetupPartyReference);
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
            GameObject character = SpawnCharacter(baseStats.GetCharacterProperties(), partyContainer);
            if (character == null) { return null; }

            // Pass stats back/forth NPC -> Character
            var characterBaseStats = character.GetComponent<BaseStats>();
            characterBaseStats.SetActiveStatSheet(baseStats.GetActiveStatSheet());
            characterBaseStats.OverrideLevel(baseStats.GetLevel());
            
            return character.GetComponent<CharacterNPCSwapper>();
        }

        public CharacterNPCSwapper SwapToNPC(Transform worldContainer)
        {
            GameObject characterNPC = SpawnNPC(baseStats.GetCharacterProperties(), worldContainer);
            if (characterNPC == null) { return null; }

            // Pass stats back/forth Character -> NPC
            var characterNPCBaseStats = characterNPC.GetComponent<BaseStats>();
            characterNPCBaseStats.SetActiveStatSheet(baseStats.GetActiveStatSheet());
            characterNPCBaseStats.OverrideLevel(baseStats.GetLevel());
            
            return characterNPC.GetComponent<CharacterNPCSwapper>();
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
