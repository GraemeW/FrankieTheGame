using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Frankie.Saving;
using Frankie.Core.Predicates;
using Frankie.Combat;
using Frankie.Control;

namespace Frankie.Stats
{
    [RequireComponent(typeof(InactiveParty))]
    public class Party : PartyBehaviour, ISaveable<PartySaveData>, IPredicateEvaluator
    {
        // State
        private readonly HashSet<CharacterProperties> unlockedCharacters = new();
        private readonly Dictionary<CharacterProperties, SceneParentReferencePair> worldNPCLookup = new();

        // Cached References
        private CombatParticipant partyLeaderCombatParticipant;
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
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            playerMover.leadAnimationParametersUpdated += UpdateLeaderAnimation;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            playerMover.leadAnimationParametersUpdated -= UpdateLeaderAnimation;
        }
        #endregion
        
        #region EventHandling
        protected override PartyAlteredData PackPartyAlteredData() => new(members, GetPartyLeaderName(), GetLeadCharacterAnimator());
        
        private void UpdateLeaderAnimation(MovementAnimationParameters movementAnimationParameters)
        {
            if (members.Count == 0) { return; }
            
            BaseStats character = members[0];
            characterSpriteLinkLookup[character].UpdateCharacterAnimation(movementAnimationParameters);
            UpdatePartySpeedAndOffsets(movementAnimationParameters.speed, movementAnimationParameters.pixelPerfectOffset);
        }
        #endregion

        #region PublicGetters
        public bool IsPartyLeader(BaseStats checkMember) => checkMember != null && (members?[0] == checkMember);
        public BaseStats GetPartyLeader() => members?[0];
        public GameObject GetPartyLeaderObject()
        {
            BaseStats partyLeader = members[0];
            return partyLeader != null ? partyLeader.gameObject : null;
        }
        public string GetPartyLeaderName() => members[0]?.GetCharacterProperties()?.GetCharacterDisplayName() ?? "";
        public int GetPartySize() => members.Count;
        public IList<CharacterProperties> GetAvailableCharactersToAdd()
        {
            List<CharacterProperties> charactersInParty = members.Select(character => character.GetCharacterProperties()).ToList();
            return unlockedCharacters.Except(charactersInParty).ToList();
        }
        #endregion

        #region PublicMethodsOther
        public void SetPartyLeader(BaseStats characterBaseStats)
        {
            if (!members.Contains(characterBaseStats)) { return; }

            members.Remove(characterBaseStats);
            members.Insert(0, characterBaseStats);

            int index = 0;
            foreach (Collider2D characterCollider2D in members.Select(partyCharacter => partyCharacter.GetComponent<Collider2D>()))
            {
                characterCollider2D.isTrigger = index != 0;
                index++;
            }
            playerMover.ResetHistory(characterBaseStats.transform.position);
            RefreshLookups();
            SyncToPartyLeaderStatusUpdates();
            TriggerMembersAltered();
        }
        
        public void ReconcilePartyLeader()
        {
            foreach (BaseStats member in members)
            {
                if (!member.TryGetComponent(out CombatParticipant combatParticipant)) { continue; }
                if (combatParticipant.IsDead()) { continue; }
                
                SetPartyLeader(member);
                return;
            }
        }

        protected override bool AddToParty(BaseStats characterBaseStats)
        {
            if (members.Count >= partyLimit) { return false; }
            if (characterBaseStats == null) { return false; } // Failsafe
            if (HasMember(characterBaseStats)) { return false; } // Verify no dupe characters to party

            members.Add(characterBaseStats);
            AddToUnlockedCharacters(characterBaseStats);
            RefreshLookups();
            inactiveParty.RestoreCharacterState(characterBaseStats); // Restore character stats, exp, equipment, inventory (if previously in party)

            if (members.Count > 1) { characterBaseStats.GetComponent<Collider2D>().isTrigger = true; }
            TriggerMembersAltered();
            return true;
        }

        public override bool AddToParty(CharacterNPCSwapper characterNPCSwapper)
        {
            // For direct interaction with world NPCs -> characters
            if (members.Count >= partyLimit) { return false; }
            if (characterNPCSwapper == null) { return false; } // Failsafe

            CharacterNPCSwapper partyCharacter = characterNPCSwapper.SwapToCharacter(container);
            UpdateWorldLookup(false, partyCharacter);
            Destroy(characterNPCSwapper.gameObject);

            return AddToParty(partyCharacter.GetBaseStats());
        }

        public override bool AddToParty(CharacterProperties characterProperties)
        {
            // For instantiation through other means (i.e. no character exists on screen)
            if (members.Count >= partyLimit) { return false; }
            if (characterProperties == null) { return false; } // Failsafe

            GameObject characterObject = CharacterNPCSwapper.SpawnCharacter(characterProperties, container);
            return characterObject != null && AddToParty(characterObject.GetComponent<BaseStats>());
        }

        public override bool RemoveFromParty(CharacterProperties characterProperties)
        {
            if (members.Count <= 1) { return false; }
            if (characterProperties == null) { return false; }

            BaseStats member = GetMember(characterProperties);
            return member != null && RemoveFromParty(member);
        }
        
        public override bool RemoveFromParty(BaseStats character)
        {
            if (members.Count <= 1) { return false; }
            if (character == null) { return false; }

            // If character to remove is leader, swap leadership to second character first
            if (IsPartyLeader(character))
            {
                SetPartyLeader(members[1]); // Guaranteed to exist because count > 1
            }

            inactiveParty.CaptureCharacterState(character);
            members.Remove(character);
            RefreshLookups();
            Destroy(character.gameObject);
            
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

        public void UpdateWorldLookup(bool addToLookUp, CharacterNPCSwapper characterNPCSwapper)
        {
            if (characterNPCSwapper == null) { return; }
            
            CharacterProperties characterProperties = characterNPCSwapper.GetBaseStats().GetCharacterProperties();
            if (addToLookUp)
            {
                string sceneReference = SceneManager.GetActiveScene().name;
                string parentName = characterNPCSwapper.transform.parent != null ? characterNPCSwapper.transform.parent.gameObject.name : string.Empty; 
                var sceneParentReferencePair = new SceneParentReferencePair(sceneReference, parentName);
                
                worldNPCLookup[characterProperties] = sceneParentReferencePair;
            }
            else
            {
                worldNPCLookup.Remove(characterProperties);
            }
        }
        
        public void AddToUnlockedCharacters(CharacterProperties characterProperties) // Callable via Unity Events
        {
            unlockedCharacters.Add(characterProperties);
        }

        public void RemoveFromUnlockedCharacters(CharacterProperties characterProperties) // Callable via Unity Events
        {
            unlockedCharacters.Remove(characterProperties);
        }
        #endregion

        #region PrivateMethods
        private Animator GetLeadCharacterAnimator() => members.Count > 0 ? characterSpriteLinkLookup[members[0]].GetAnimator() : null;
        
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

        private void SyncToPartyLeaderStatusUpdates()
        {
            if (partyLeaderCombatParticipant != null) { partyLeaderCombatParticipant.UnsubscribeToStateUpdates(HandlePartyLeaderStatusUpdate); }
            if (members is not { Count: > 0 }) { return; }
            
            partyLeaderCombatParticipant = members[0].GetComponent<CombatParticipant>();
            partyLeaderCombatParticipant.SubscribeToStateUpdates(HandlePartyLeaderStatusUpdate);
        }

        private void HandlePartyLeaderStatusUpdate(StateAlteredInfo stateAlteredInfo)
        {
            if (stateAlteredInfo == null) { return; }
            if (stateAlteredInfo.stateAlteredType != StateAlteredType.Dead) { return; }
            if (members is not { Count: > 1 }) { return; }

            ReconcilePartyLeader();
        }
        #endregion

        #region PredicateInterface
        public bool? Evaluate(Predicate predicate)
        {
            var predicateParty = predicate as PredicateParty;
            return predicateParty != null ? predicateParty.Evaluate(this) : null;
        }
        #endregion
        
        #region SaveInterface
        public LoadPriority GetLoadPriority() => LoadPriority.ObjectInstantiation;
        
        public SaveState CaptureState()
        {
            members ??= new List<BaseStats>();
            List<CharacterProperties> partyCharacters = members.Select(character => character.GetCharacterProperties()).ToList();
            var partySaveData = new PartySaveData(partyCharacters, unlockedCharacters, worldNPCLookup);
            return ManualGetStateFromData(partySaveData);
        }

        public void RestoreState(SaveState saveState)
        {
            if (saveState.GetState(typeof(PartySerializableSaveData)) is not PartySerializableSaveData partySerializableSaveData) { return; }
            PartySaveData partySaveData = UnpackPartySerializableSaveData(partySerializableSaveData);

            RestorePartyMembers(partySaveData.partyCharacters);
            RestoreUnlockedCharacters(partySaveData.unlockedCharacters);
            RestoreWorldNPCs(partySaveData.worldNPCLookup);
        }
        
        public SaveState ManualGetStateFromData(PartySaveData data)
        {
            List<string> partyNames = data.GetPartyCharacterNames();
            List<string> unlockedCharacterNames = data.GetUnlockedCharacterNames();
            
            Dictionary<CharacterProperties, SceneParentReferencePair> localNPCWorldLookup = data.worldNPCLookup;
            var worldNPCNameLookup = new Dictionary<string, SceneParentReferencePair>();
            foreach (KeyValuePair<CharacterProperties, SceneParentReferencePair> pair in localNPCWorldLookup) { worldNPCNameLookup[pair.Key.GetCharacterID()] = pair.Value; }
            
            var partySerializableSaveData = new PartySerializableSaveData(partyNames, unlockedCharacterNames, worldNPCNameLookup);
            return new SaveState(GetLoadPriority(), partySerializableSaveData);
        }

        public PartySaveData ManualGetDataFromState(SaveState saveState)
        {
            return saveState?.GetState(typeof(PartySerializableSaveData)) is PartySerializableSaveData partySerializableSaveData ? UnpackPartySerializableSaveData(partySerializableSaveData) : new PartySaveData();
        }
        
        private static PartySaveData UnpackPartySerializableSaveData(PartySerializableSaveData partySerializableSaveData)
        {
            if (partySerializableSaveData == null) { return new PartySaveData(); }
            
            List<CharacterProperties> localPartyCharacters = partySerializableSaveData.partyCharacterNames.Select(CharacterProperties.GetCharacterPropertiesFromName).Where(characterProperties => characterProperties != null).ToList();
            HashSet<CharacterProperties> localUnlockedCharacters = partySerializableSaveData.unlockedCharacterNames.Select(CharacterProperties.GetCharacterPropertiesFromName).Where(unlockedCharacter => unlockedCharacter != null).ToHashSet();
            var localWorldNPCLookup = new Dictionary<CharacterProperties, SceneParentReferencePair>();
            foreach (KeyValuePair<string, SceneParentReferencePair> pair in partySerializableSaveData.worldNPCNameLookup)
            {
                CharacterProperties characterProperties = CharacterProperties.GetCharacterPropertiesFromName(pair.Key);
                if (characterProperties == null) { continue; }
                localWorldNPCLookup[characterProperties] = pair.Value;
            }
            
            return new PartySaveData(localPartyCharacters, localUnlockedCharacters, localWorldNPCLookup);
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
            SyncToPartyLeaderStatusUpdates();
            TriggerMembersAltered();
        }

        private void RestoreUnlockedCharacters(HashSet<CharacterProperties> localUnlockedCharacters)
        {
            unlockedCharacters.Clear();
            if (localUnlockedCharacters == null) { return; }
            
            foreach (CharacterProperties characterProperties in localUnlockedCharacters)
            {
                AddToUnlockedCharacters(characterProperties);
            }
        }

        private void RestoreWorldNPCs(Dictionary<CharacterProperties, SceneParentReferencePair> localWorldNPCLookup)
        {
            worldNPCLookup.Clear();
            if (localWorldNPCLookup == null) { return; }
            
            foreach (var pair in localWorldNPCLookup.Where(pair => pair.Key != null)) { worldNPCLookup[pair.Key] = pair.Value; }
            foreach (KeyValuePair<CharacterProperties, SceneParentReferencePair> worldNPCEntry in worldNPCLookup)
            {
                if (worldNPCEntry.Value.sceneName != SceneManager.GetActiveScene().name) { continue; }
                    
                GameObject parent = GameObject.Find(worldNPCEntry.Value.parentName);
                Transform parentTransform = parent != null ? parent.transform : null;
                CharacterNPCSwapper.SpawnNPC(worldNPCEntry.Key, parentTransform);
            }
        }
        #endregion
    }
}
