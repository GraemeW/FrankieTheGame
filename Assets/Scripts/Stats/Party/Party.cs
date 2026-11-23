using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using LowDefMustard.Saving;
using Frankie.Core;
using Frankie.Control;
using Frankie.Combat;

namespace Frankie.Stats
{
    [RequireComponent(typeof(InactiveParty))]
    public class Party : PartyBehaviour, ISaveable<PartySaveData>
    {
        // State
        private readonly HashSet<string> unlockedCharacterIDs = new();
        private readonly Dictionary<string, SceneParentReferencePair> worldNPCLookup = new();

        // Cached References
        private InactiveParty inactiveParty;

        #region UnityMethods
        protected override void Awake()
        {
            inactiveParty = GetComponent<InactiveParty>();
            base.Awake();
        }

        private void Start()
        {
            InitializeUnlockedCharacters();
            ReconcileTheDead();
            TriggerMembersAltered();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            playerStateMachine.playerStateChanged += HandlePlayerStateUpdates;
            playerMover.leadAnimationParametersUpdated += UpdateLeaderAnimation;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            playerStateMachine.playerStateChanged -= HandlePlayerStateUpdates;
            playerMover.leadAnimationParametersUpdated -= UpdateLeaderAnimation;
        }
        #endregion
        
        #region EventHandling
        protected override PartyAlteredData PackPartyAlteredData() => new(PartyBehaviourType.Party, members, GetPartyLeaderName(), GetLeadCharacterAnimator());

        private void HandlePlayerStateUpdates(PlayerStateType playerStateType, IPlayerStateInfo playerStateInfo)
        {
            if (playerStateType != PlayerStateType.InWorld) { return; }
            if (ReconcileTheDead()) { TriggerMembersAltered(); }
        }
        
        private void UpdateLeaderAnimation(MovementAnimationParameters movementAnimationParameters)
        {
            if (members.Count == 0) { return; }
            
            BaseStats character = members[0];
            characterMoveLinkLookup[character].UpdateCharacterAnimation(movementAnimationParameters);
            UpdatePartySpeedAndOffsets(movementAnimationParameters.speed, movementAnimationParameters.pixelPerfectOffset);
        }
        #endregion

        #region PublicGetters
        public IList<CharacterProperties> GetAvailableCharactersToAdd() => CharacterProperties.GetCharacterPropertiesFromNames(unlockedCharacterIDs).Where(characterProperties => !HasMember(characterProperties)).ToList();
        #endregion

        #region PublicMethodsOther
        public void SetPartyLeader(BaseStats characterBaseStats, bool announceUpdate = true)
        {
            if (characterBaseStats == null) { return; }
            if (combatParticipantLookup.TryGetValue(characterBaseStats, out CombatParticipant combatParticipant) && combatParticipant.IsDead()) { return; }  
            if (!members.Contains(characterBaseStats)) { return; }

            members.Remove(characterBaseStats);
            members.Insert(0, characterBaseStats);
            
            playerMover.ResetHistory(characterBaseStats.transform.position);
            RefreshColliders();
            RefreshLookups();
            if (announceUpdate) { TriggerMembersAltered(); }
        }

        protected override bool AddToParty(BaseStats characterBaseStats)
        {
            if (characterBaseStats == null) { return false; }
            if (members.Count >= partyLimit) { return false; }
            if (HasMember(characterBaseStats)) { return false; }
            
            members.Add(characterBaseStats);
            AddToUnlockedCharacters(characterBaseStats);

            inactiveParty.RestoreCharacterState(characterBaseStats); // Restore character stats, exp, equipment, inventory (if previously in party)
            
            ReconcileTheDead();
            RefreshLookups();
            RefreshColliders();
            if (TryGetPartyLeader(out BaseStats partyLeader)) { characterBaseStats.transform.position = partyLeader.transform.position; } // Overlap position on Add
            
            TriggerMembersAltered();
            return true;
        }

        public override bool AddToParty(CharacterNPCSwapper characterNPCSwapper)
        {
            // For direct interaction with world NPCs -> characters
            if (characterNPCSwapper == null) { return false; }
            if (members.Count >= partyLimit) { return false; }

            CharacterNPCSwapper partyCharacter = characterNPCSwapper.SwapToCharacter(container);
            if (partyCharacter == null) { return false; }
            if (!AddToParty(partyCharacter.GetBaseStats())) { Destroy(partyCharacter.gameObject); return false; } // Keep the world NPC on failure

            UpdateWorldLookup(false, partyCharacter);
            Destroy(characterNPCSwapper.gameObject);
            return true;
        }

        public override bool AddToParty(CharacterProperties characterProperties)
        {
            // For instantiation through other means (i.e. no character exists on screen)
            if (characterProperties == null) { return false; }
            if (members.Count >= partyLimit) { return false; }
            if (HasMember(characterProperties)) { return false; }

            GameObject characterObject = CharacterNPCSwapper.SpawnCharacter(characterProperties, container);
            if (characterObject != null && characterObject.TryGetComponent(out BaseStats baseStats) && AddToParty(baseStats))
            {
                return true;
            }
            
            Destroy(characterObject);
            return false;
        }

        public override bool RemoveFromParty(BaseStats character)
        {
            if (members.Count <= 1) { return false; }
            if (character == null) { return false; }

            // If character to remove is leader, swap leadership to second character first
            if (IsPartyLeader(character))
            {
                foreach (BaseStats characterBaseStats in members.Where(characterBaseStats => characterBaseStats != character))
                {
                    if (combatParticipantLookup.TryGetValue(characterBaseStats, out CombatParticipant combatParticipant) && combatParticipant.IsDead()) { continue; }
                    SetPartyLeader(characterBaseStats, false);
                    break;
                }
            }

            inactiveParty.CaptureCharacterState(character);
            members.Remove(character);
            Destroy(character.gameObject);
            
            ReconcileTheDead();
            RefreshLookups();
            RefreshColliders();
            
            TriggerMembersAltered();
            return true;
        }
        
        public override bool RemoveFromParty(BaseStats character, Transform worldTransform)
        {
            if (members.Count <= 1) { return false; }
            if (character == null) { return false; } // Failsafe

            // Instantiates an NPC at defined location
            var partyCharacter = character.GetComponent<CharacterNPCSwapper>();
            if (partyCharacter == null) { return false; }

            CharacterNPCSwapper worldNPC = partyCharacter.SwapToNPC(worldTransform);
            if (worldNPC == null) { return false; }
            UpdateWorldLookup(true, worldNPC);

            return RemoveFromParty(character);
        }
        
        public override bool RemoveFromParty(CharacterProperties characterProperties)
        {
            if (members.Count <= 1) { return false; }
            if (characterProperties == null) { return false; }

            BaseStats member = GetMember(characterProperties);
            return member != null && RemoveFromParty(member);
        }

        public void UpdateWorldLookup(bool addToLookUp, CharacterNPCSwapper characterNPCSwapper)
        {
            if (characterNPCSwapper == null) { return; }
            
            CharacterProperties characterProperties = characterNPCSwapper.GetBaseStats().GetCharacterProperties();
            if (characterProperties == null) { return; }
            
            if (addToLookUp)
            {
                string sceneReference = SceneManager.GetActiveScene().name;
                string parentName = characterNPCSwapper.transform.parent != null ? characterNPCSwapper.transform.parent.gameObject.name : string.Empty; 
                var sceneParentReferencePair = new SceneParentReferencePair(sceneReference, parentName);
                
                worldNPCLookup[characterProperties.GetCharacterID()] = sceneParentReferencePair;
            }
            else
            {
                worldNPCLookup.Remove(characterProperties.GetCharacterID());
            }
        }
        
        public void AddToUnlockedCharacters(CharacterProperties characterProperties) // Callable via Unity Events
        {
            if (characterProperties == null) { return; }
            unlockedCharacterIDs.Add(characterProperties.GetCharacterID());
        }

        public void RemoveFromUnlockedCharacters(CharacterProperties characterProperties) // Callable via Unity Events
        {
            if (characterProperties == null) { return; }
            unlockedCharacterIDs.Remove(characterProperties.GetCharacterID());
        }
        #endregion

        #region PrivateMethods
        private Animator GetLeadCharacterAnimator() => members.Count > 0 ? characterMoveLinkLookup[members[0]].GetAnimator() : null;
        
        private void InitializeUnlockedCharacters()
        {
            foreach (BaseStats baseStats in members)
            {
                AddToUnlockedCharacters(baseStats);
            }
        }

        private void AddToUnlockedCharacters(BaseStats character)
        {
            CharacterProperties characterProperties = character.GetCharacterProperties();
            AddToUnlockedCharacters(characterProperties);
        }

        private void RemoveFromUnlockedCharacters(BaseStats character)
        {
            CharacterProperties characterProperties = character.GetCharacterProperties();
            RemoveFromUnlockedCharacters(characterProperties);
        }
        
        private bool ReconcileTheDead()
        {
            // N.B.  Does NOT trigger membersAltered event, must be triggered separately
            
            BaseStats newLeader = null;
            List<BaseStats> deadMembers = new List<BaseStats>();
            foreach (BaseStats member in members)
            {
                if (!combatParticipantLookup.TryGetValue(member, out CombatParticipant combatParticipant)) { continue; }
                if (combatParticipant.IsDead())
                {
                    deadMembers.Add(member);
                    continue;
                }
                if (newLeader == null) { newLeader = member; }
            }

            bool stateModified = false;
            if (newLeader != null && newLeader != members[0])
            {
                stateModified = true;
                SetPartyLeader(newLeader, false); }
            if (deadMembers.Count > 0)
            {
                stateModified = true;
                members.RemoveAll(deadMembers.Contains);
                members.AddRange(deadMembers);
            }
            return stateModified;
        }
        #endregion
        
        #region SaveInterface
        public LoadPriority GetLoadPriority() => LoadPriority.ObjectInstantiation;

        public void ApplyFinishingTouches()
        {
            ReconcileTheDead();
            TriggerMembersAltered();
        }

        public SaveState CaptureState()
        {
            members ??= new List<BaseStats>();
            List<string> partyCharacterNames = CharacterProperties.GetCharacterIDs(members.Select(character => character.GetCharacterProperties()));
            var partySerializableSaveData = new PartySerializableSaveData(partyCharacterNames, unlockedCharacterIDs.ToList(), new Dictionary<string, SceneParentReferencePair>(worldNPCLookup));
            return new SaveState(GetLoadPriority(), partySerializableSaveData);
        }

        public void RestoreState(SaveState saveState)
        {
            if (saveState == null || !saveState.TryGetState(out PartySerializableSaveData partySerializableSaveData) || partySerializableSaveData == null) { return; }

            RestorePartyMembers(CharacterProperties.GetCharacterPropertiesFromNames(partySerializableSaveData.partyCharacterNames));
            RestoreUnlockedCharacters(partySerializableSaveData.unlockedCharacterNames);
            RestoreWorldNPCs(partySerializableSaveData.worldNPCNameLookup);
        }
        
        public SaveState ManualGetStateFromData(PartySaveData data) => new(GetLoadPriority(), data.ToSerializableSaveData());

        public bool TryManualGetDataFromState(SaveState saveState, out PartySaveData data)
        {
            data = new PartySaveData();
            // Default Pass Back
            if (saveState == null || !saveState.TryGetState(out PartySerializableSaveData partySerializableSaveData)) { return true; }
            
            // Save Found Pass Back
            data = PartySaveData.FromSerializableSaveData(partySerializableSaveData);
            return true;
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
            RefreshLookups();
            TriggerMembersAltered();
        }

        private void RestoreUnlockedCharacters(List<string> unlockedCharacterNames)
        {
            unlockedCharacterIDs.Clear();
            foreach (CharacterProperties characterProperties in CharacterProperties.GetCharacterPropertiesFromNames(unlockedCharacterNames))
            {
                AddToUnlockedCharacters(characterProperties);
            }
        }

        private void RestoreWorldNPCs(Dictionary<string, SceneParentReferencePair> worldNPCNameLookup)
        {
            worldNPCLookup.Clear();
            if (worldNPCNameLookup == null) { return; }
            
            foreach ((string characterID, SceneParentReferencePair sceneParentReferencePair) in worldNPCNameLookup)
            {
                if (!CharacterProperties.TryGetCharacterPropertiesFromName(characterID, out CharacterProperties characterProperties)) { continue; }
                
                worldNPCLookup[characterID] = sceneParentReferencePair;
                if (sceneParentReferencePair.sceneName != SceneManager.GetActiveScene().name) { continue; }
                    
                GameObject parent = GameObject.Find(sceneParentReferencePair.parentName);
                Transform parentTransform = parent != null ? parent.transform : null;
                CharacterNPCSwapper.SpawnNPC(characterProperties, parentTransform);
            }
        }
        #endregion
    }
}
