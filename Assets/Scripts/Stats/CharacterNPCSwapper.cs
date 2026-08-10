using UnityEngine;
using LowDefMustard.Utils;
using Frankie.Core;
using Frankie.Saving;

namespace Frankie.Stats
{
    [RequireComponent(typeof(BaseStats))]
    public class CharacterNPCSwapper : MonoBehaviour, ISaveableBase
    {
        // Cached References
        private BaseStats baseStats;
        private ReInitLazyValue<Party> party;
        private ReInitLazyValue<PartyAssist> partyAssist;

        #region StaticMethods
        private static Party SetupPartyReference()
        {
            GameObject playerObject = Player.FindPlayerObject();
            return playerObject != null ? playerObject.GetComponent<Party>() : null;
        }

        private static PartyAssist SetupPartyAssistReference()
        {
            GameObject playerObject = Player.FindPlayerObject();
            return playerObject != null ? playerObject.GetComponent<PartyAssist>() : null;
        }

        public static GameObject SpawnCharacter(CharacterProperties characterProperties, Transform partyTransform)
        {
            if (characterProperties == null || partyTransform == null)
            {
                return null;
            }

            GameObject characterPrefab = characterProperties.GetCharacterPrefab();
            if (characterPrefab == null)
            {
                return null;
            }

            GameObject character = Instantiate(characterPrefab, partyTransform);
            return character;
        }

        public static GameObject SpawnNPC(CharacterProperties characterProperties, Transform worldContainer)
        {
            if (characterProperties == null)
            {
                return null;
            }

            GameObject characterNPCPrefab = characterProperties.GetCharacterNPCPrefab();
            if (characterNPCPrefab == null)
            {
                return null;
            }

            GameObject characterNPC = worldContainer != null ? Instantiate(characterNPCPrefab, worldContainer) : Instantiate(characterNPCPrefab);
            return characterNPC;
        }

        #endregion

        #region UnityMethods
        private void Awake()
        {
            SetupCachedReferences();
        }

        private void Start()
        {
            party.ForceInit();
            partyAssist.ForceInit();
        }

        private void OnEnable()
        {
            DisableNPCIfInParty();
        }

        private void SetupCachedReferences()
        {
            baseStats = GetComponent<BaseStats>();
            party ??= new ReInitLazyValue<Party>(SetupPartyReference);
            partyAssist ??= new ReInitLazyValue<PartyAssist>(SetupPartyAssistReference);
        }
        #endregion

        #region PublicMethods
        public BaseStats GetBaseStats() => baseStats;

        public CharacterNPCSwapper SwapToCharacter(Transform partyContainer)
        {
            GameObject character = SpawnCharacter(baseStats.GetCharacterProperties(), partyContainer);
            if (character == null)
            {
                return null;
            }

            // Pass stats back/forth NPC -> Character
            var characterBaseStats = character.GetComponent<BaseStats>();
            characterBaseStats.SetActiveStatSheet(baseStats.GetActiveStatSheet());
            characterBaseStats.OverrideLevel(baseStats.GetLevel());

            return character.GetComponent<CharacterNPCSwapper>();
        }

        public CharacterNPCSwapper SwapToNPC(Transform worldContainer)
        {
            GameObject characterNPC = SpawnNPC(baseStats.GetCharacterProperties(), worldContainer);
            if (characterNPC == null)
            {
                return null;
            }

            // Pass stats back/forth Character -> NPC
            var characterNPCBaseStats = characterNPC.GetComponent<BaseStats>();
            characterNPCBaseStats.SetActiveStatSheet(baseStats.GetActiveStatSheet());
            characterNPCBaseStats.OverrideLevel(baseStats.GetLevel());

            return characterNPC.GetComponent<CharacterNPCSwapper>();
        }

        public void JoinParty(PlayerStateMachine playerStateMachine) // Called via Unity Events
        {
            if (playerStateMachine.TryGetComponent(out Party localParty))
            {
                localParty.AddToParty(this);
            }
        }

        public void JoinPartyAssist(PlayerStateMachine playerStateMachine) // Called via Unity Events
        {
            if (playerStateMachine.TryGetComponent(out PartyAssist localPartyAssist))
            {
                localPartyAssist.AddToParty(this);
            }
        }
        #endregion

        #region PrivateMethods
        private void DisableNPCIfInParty()
        {
            CharacterProperties characterProperties = baseStats.GetCharacterProperties();
            if (characterProperties == null) { return; }
            
            if (party.value != null)
            {
                BaseStats characterInParty = party.value.GetMember(characterProperties);
                if (characterInParty != null && characterInParty != baseStats)
                {
                    gameObject.SetActive(false);
                }
            }

            if (partyAssist.value != null)
            {
                BaseStats characterInPartyAssist = partyAssist.value.GetMember(characterProperties);
                if (characterInPartyAssist != null && characterInPartyAssist != baseStats)
                {
                    gameObject.SetActive(false);
                }
            }
        }
        #endregion
        
        #region SaveInterface
        public LoadPriority GetLoadPriority() => LoadPriority.ObjectProperty;

        public SaveState CaptureState() => null;

        public void RestoreState(SaveState saveState) { }

        public void ApplyFinishingTouches()
        {
            if (!gameObject.activeSelf) { return; }
            
            SetupCachedReferences();
            party.ForceInit();
            partyAssist.ForceInit();
            
            DisableNPCIfInParty();
        }
        #endregion
    }
}
