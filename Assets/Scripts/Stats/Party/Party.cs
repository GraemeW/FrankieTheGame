using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Frankie.Saving;
using Frankie.Core;

namespace Frankie.Stats
{
    [RequireComponent(typeof(InactiveParty))]
    public class Party : PartyBehaviour, ISaveable, IPredicateEvaluator
    {
        // State
        private readonly List<CharacterProperties> unlockedCharacters = new();
        private Dictionary<string, SceneParentPair> worldNPCLookup = new();

        // Cached References
        private InactiveParty inactiveParty;

        // Events
        public event Action partyUpdated;

        // DataStructures
        [Serializable]
        private struct SceneParentPair
        {
            public string sceneName;
            public string parentName;

            public SceneParentPair(string sceneName, string parentName)
            {
                this.sceneName = sceneName;
                this.parentName = parentName;
            }
        }

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
            playerMover.leaderAnimatorUpdated += UpdateLeaderAnimation;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            playerMover.leaderAnimatorUpdated -= UpdateLeaderAnimation;
        }
        #endregion

        #region PublicGetters
        public BaseStats GetPartyLeader() => members[0];
        public string GetPartyLeaderName() => members[0].GetCharacterProperties().GetCharacterNamePretty();
        public Animator GetLeadCharacterAnimator() => members.Count > 0 ? characterSpriteLinkLookup[members[0]].GetAnimator() : null;
        public int GetPartySize() => members.Count;
        public List<CharacterProperties> GetAvailableCharactersToAdd()
        {
            List<CharacterProperties> charactersInParty = new List<CharacterProperties>();
            foreach (BaseStats character in members)
            {
                charactersInParty.Add(character.GetCharacterProperties());
            }

            return unlockedCharacters.Except(charactersInParty).ToList();
        }
        #endregion

        #region PublicMethodsOther
        public void SetPartyLeader(BaseStats character)
        {
            if (!members.Contains(character)) { return; }

            members.Remove(character);
            members.Insert(0, character);

            int index = 0;
            foreach (Collider2D characterCollider2D in members.Select(partyCharacter => partyCharacter.GetComponent<Collider2D>()))
            {
                characterCollider2D.isTrigger = index != 0;
                index++;
            }
            playerMover.ResetHistory(character.transform.position);
            RefreshAnimatorLookup();
            partyUpdated?.Invoke();
        }

        protected override bool AddToParty(BaseStats character)
        {
            if (members.Count >= partyLimit) { return false; }
            if (character == null) { return false; } // Failsafe
            if (HasMember(character)) { return false; } // Verify no dupe characters to party

            members.Add(character);
            AddToUnlockedCharacters(character);
            RefreshAnimatorLookup();
            inactiveParty.RestoreCharacterState(ref character); // Restore character stats, exp, equipment, inventory (if previously in party)
            inactiveParty.RemoveFromInactiveStorage(character); // Stop tracking in inactive storage (i.e. since in active party)

            if (members.Count > 1) { character.GetComponent<Collider2D>().isTrigger = true; }
            partyUpdated?.Invoke();
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

            GameObject characterObject = CharacterNPCSwapper.SpawnCharacter(characterProperties.name, container);

            BaseStats character = characterObject.GetComponent<BaseStats>();
            return AddToParty(character);
        }

        public override bool RemoveFromParty(BaseStats character)
        {
            if (members.Count <= 1) { return false; }
            if (character == null) { return false; } // Failsafe

            // If character to remove is leader, swap leadership to second character first
            if (IsPartyLeader(character))
            {
                SetPartyLeader(members[1]); // Guaranteed to exist because count > 1
            }

            inactiveParty.CaptureCharacterState(character);
            members.Remove(character);
            characterSpriteLinkLookup.Remove(character);

            Destroy(character.gameObject);
            partyUpdated?.Invoke();

            return true;
        }

        public override bool RemoveFromParty(CharacterProperties characterProperties)
        {
            if (members.Count <= 1) { return false; }
            if (characterProperties == null) { return false; } // Failsafe

            BaseStats member = GetMember(characterProperties);
            return member != null && RemoveFromParty(member);
        }

        public override bool RemoveFromParty(BaseStats character, Transform worldTransform)
        {
            if (members.Count <= 1) { return false; }
            if (character == null) { return false; } // Failsafe

            // Instantiates an NPC at defined location
            CharacterNPCSwapper partyCharacter = character.GetComponent<CharacterNPCSwapper>();
            if (partyCharacter == null) { return false; }

            CharacterNPCSwapper worldNPC = partyCharacter.SwapToNPC(worldTransform);
            UpdateWorldLookup(true, worldNPC);

            return RemoveFromParty(character);
        }

        public void UpdateWorldLookup(bool addToLookUp, CharacterNPCSwapper characterNPCSwapper)
        {
            string characterName = characterNPCSwapper.GetBaseStats().GetCharacterProperties().name;
            if (addToLookUp)
            {
                string sceneReference = SceneManager.GetActiveScene().name;
                SceneParentPair sceneParentPair = new SceneParentPair(sceneReference, characterNPCSwapper.transform.parent.gameObject.name);
                worldNPCLookup[characterName] = sceneParentPair;
            }
            else
            {
                worldNPCLookup.Remove(characterName);
            }
        }
        #endregion

        #region PrivateMethods
        private bool IsPartyLeader(BaseStats combatParticipant) => members[0] == combatParticipant;
        
        private void UpdateLeaderAnimation(float speed, float xLookDirection, float yLookDirection)
        {
            if (members.Count == 0) { return; }

            BaseStats character = members[0];
            characterSpriteLinkLookup[character].UpdateCharacterAnimation(xLookDirection, yLookDirection, speed);
            UpdatePartySpeed(speed);
        }
        
        private void InitializeUnlockedCharacters()
        {
            foreach (BaseStats combatParticipant in members)
            {
                AddToUnlockedCharacters(combatParticipant);
            }
        }

        private void AddToUnlockedCharacters(BaseStats character)
        {
            CharacterProperties characterProperties = character.GetCharacterProperties();
            AddToUnlockedCharacters(characterProperties);
        }

        private void AddToUnlockedCharacters(CharacterProperties characterProperties)
        {
            if (!unlockedCharacters.Contains(characterProperties))
            {
                unlockedCharacters.Add(characterProperties);
            }
        }

        private void RemoveFromUnlockedCharacters(BaseStats character)
        {
            CharacterProperties characterProperties = character.GetCharacterProperties();
            unlockedCharacters.Remove(characterProperties);
        }
        #endregion

        #region Interfaces
        [Serializable]
        private class PartySaveData
        {
            public List<string> partyStrings;
            public List<string> unlockedCharacterStrings;
            public Dictionary<string, SceneParentPair> worldNPCLookup;

            public PartySaveData(List<string> currentParty, List<string> unlockedCharacterStrings, Dictionary<string, SceneParentPair> worldNPCLookup)
            {
                partyStrings = currentParty;
                this.unlockedCharacterStrings = unlockedCharacterStrings;
                this.worldNPCLookup = worldNPCLookup;
            }
        }
        
        public LoadPriority GetLoadPriority() => LoadPriority.ObjectInstantiation;
        public SaveState CaptureState()
        {
            var currentPartyStrings = members.Select(character => character.GetCharacterProperties().name).ToList();
            var unlockedCharacterStrings = unlockedCharacters.Select(characterProperties => characterProperties.name).ToList();

            var partySaveState = new PartySaveData(currentPartyStrings, unlockedCharacterStrings, worldNPCLookup);
            var saveState = new SaveState(GetLoadPriority(), partySaveState);
            return saveState;
        }

        public void RestoreState(SaveState saveState)
        {
            if (saveState.GetState(typeof(PartySaveData)) is not PartySaveData partySaveData) { return; }

            // Clear characters in existing party in scene
            foreach (BaseStats character in members) { Destroy(character.gameObject); }
            members.Clear();

            // Pull characters from save
            List<string> addPartyStrings = partySaveData.partyStrings;
            if (addPartyStrings != null)
            {
                foreach (string characterName in addPartyStrings)
                {
                    if (members.Count > partyLimit) { break; } // Failsafe

                    GameObject characterObject = CharacterNPCSwapper.SpawnCharacter(characterName, container);
                    if (characterObject == null) { continue; }

                    var character = characterObject.GetComponent<BaseStats>();
                    if (character == null) { Destroy(characterObject); continue; }

                    members.Add(character);

                    if (members.Count > 1) { characterObject.GetComponent<Collider2D>().isTrigger = true; }
                }
                RefreshAnimatorLookup();
            }
            partyUpdated?.Invoke();

            // Build up unlocked characters list
            List<string> unlockedCharacterStrings = partySaveData.unlockedCharacterStrings;
            if (unlockedCharacterStrings != null)
            {
                foreach (string characterName in unlockedCharacterStrings)
                {
                    CharacterProperties characterProperties = CharacterProperties.GetCharacterPropertiesFromName(characterName);
                    AddToUnlockedCharacters(characterProperties);
                }
            }

            // Instantiate world NPCs
            worldNPCLookup = partySaveData.worldNPCLookup;
            if (worldNPCLookup != null)
            {
                foreach (KeyValuePair<string, SceneParentPair> worldNPCEntry in worldNPCLookup)
                {
                    if (worldNPCEntry.Value.sceneName != SceneManager.GetActiveScene().name) continue;
                    GameObject parent = GameObject.Find(worldNPCEntry.Value.parentName);
                    if (parent == null) { return; }

                    CharacterNPCSwapper.SpawnNPC(worldNPCEntry.Key, parent.transform);
                }
            }
        }

        public bool? Evaluate(Predicate predicate)
        {
            var predicateParty = predicate as PredicateParty;
            return predicateParty != null ? predicateParty.Evaluate(this) : null;
        }
        #endregion
    }
}
