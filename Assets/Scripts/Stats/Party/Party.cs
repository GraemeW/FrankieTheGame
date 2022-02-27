using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frankie.Combat;
using Frankie.Control;
using Frankie.Utils;
using System;
using Frankie.Saving;
using UnityEngine.SceneManagement;
using System.Linq;
using Frankie.Core;

namespace Frankie.Stats
{
    [RequireComponent(typeof(PlayerMover))]
    [RequireComponent(typeof(InactiveParty))]
    public class Party : MonoBehaviour, ISaveable, IPredicateEvaluator
    {
        // Tunables
        [SerializeField][Range(1,4)] int partyLimit = 4;
        [SerializeField] List<CombatParticipant> party = new List<CombatParticipant>();
        [SerializeField] Transform partyContainer = null;
        [SerializeField] int partyOffset = 16;

        // State
        List<CharacterProperties> unlockedCharacters = new List<CharacterProperties>();
        Dictionary<string, Tuple<string, string>> worldNPCLookup = new Dictionary<string, Tuple<string, string>>();
        Dictionary<CombatParticipant, Animator> animatorLookup = new Dictionary<CombatParticipant, Animator>();

        // Cached References
        PlayerMover playerMover = null;
        InactiveParty inactiveParty = null;

        // Events
        public event Action partyUpdated;

        #region UnityMethods
        private void Awake()
        {
            playerMover = GetComponent<PlayerMover>();
            inactiveParty = GetComponent<InactiveParty>();
            RefreshAnimatorLookup();
        }

        private void Start()
        {
            InitializeUnlockedCharacters();
        }

        private void OnEnable()
        {
            playerMover.movementHistoryReset += ResetPartyOffsets;
            playerMover.leaderAnimatorUpdated += UpdateLeaderAnimation;
            playerMover.playerMoved += UpdatePartyOffsets;
        }

        private void OnDisable()
        {
                playerMover.movementHistoryReset -= ResetPartyOffsets;
                playerMover.leaderAnimatorUpdated -= UpdateLeaderAnimation;
                playerMover.playerMoved -= UpdatePartyOffsets;
        }
        #endregion

        #region PublicGetters
        public CombatParticipant GetPartyLeader()
        {
            return party[0];
        }

        public string GetPartyLeaderName()
        {
            return party[0].GetCombatName();
        }

        public bool IsPartyLeader(CombatParticipant combatParticipant)
        {
            return party[0] == combatParticipant;
        }

        public Animator GetLeadCharacterAnimator()
        {
            return animatorLookup[party[0]];
        }

        public List<CombatParticipant> GetParty()
        {
            return party;
        }

        public int GetPartySize()
        {
            return party.Count;
        }

        public bool HasMember(CombatParticipant member)
        {
            CharacterProperties characterProperties = member.GetBaseStats()?.GetCharacterProperties();
            return HasMember(characterProperties);
        }

        public bool HasMember(CharacterProperties member)
        {
            foreach (CombatParticipant character in party)
            {
                CharacterProperties characterProperties = character.GetBaseStats()?.GetCharacterProperties();
                if (characterProperties.GetCharacterNameID() == member.GetCharacterNameID())
                {
                    return true;
                }
            }
            return false;
        }

        public bool IsAnyMemberAlive()
        {
            bool alive = false;
            foreach (CombatParticipant combatParticipant in party)
            {
                if (!combatParticipant.IsDead()) { alive = true; }
            }
            return alive;
        }

        public CombatParticipant GetMember(CharacterProperties member)
        {
            foreach (CombatParticipant combatParticipant in party)
            {
                CharacterProperties characterProperties = combatParticipant.GetBaseStats().GetCharacterProperties();
                if (characterProperties.GetCharacterNameID() == member.GetCharacterNameID())
                {
                    return combatParticipant;
                }
            }
            return null;
        }

        public CombatParticipant GetNextMember(CombatParticipant currentMember, bool traverseForward)
        {
            CombatParticipant nextMember = null;

            // Simple case
            if (currentMember == null || party.Count == 1)
            {
                nextMember = party[0];
            }
            else
            {
                // Normal handling
                for (int index = 0; index < party.Count; index++)
                {
                    if (currentMember != party[index]) { continue; }

                    if (traverseForward)
                    {
                        if (index == party.Count - 1) { nextMember = party[0]; }
                        else { nextMember = party[index + 1]; }
                    }
                    else
                    {
                        if (index == 0) { nextMember = party[party.Count - 1]; }
                        else { nextMember = party[index - 1]; }
                    }
                }
            }

            return nextMember;
        }

        public List<CharacterProperties> GetAvailableCharactersToAdd()
        {
            List<CharacterProperties> charactersInParty = new List<CharacterProperties>();
            foreach (CombatParticipant character in party)
            {
                charactersInParty.Add(character.GetBaseStats().GetCharacterProperties());
            }

            return unlockedCharacters.Except(charactersInParty).ToList();
        }

        #endregion

        #region PublicMethodsOther
        public void SetPartyLeader(CombatParticipant character)
        {
            if (!party.Contains(character)) { return; }

            party.Remove(character);
            party.Insert(0, character);

            int index = 0;
            foreach (CombatParticipant partyCharacter in party)
            {
                Collider2D collider2D = partyCharacter.GetComponent<Collider2D>();
                collider2D.isTrigger = index == 0 ? false : true;
                index++;
            }
            playerMover.ResetHistory(character.transform.position);
            RefreshAnimatorLookup();
            partyUpdated?.Invoke();
        }

        // AddToParty -- Parent
        public bool AddToParty(CombatParticipant character)
        {
            if (party.Count >= partyLimit) { return false; }
            if (character == null) { return false; } // Failsafe
            if (HasMember(character)) { return false; } // Verify no dupe characters to party

            party.Add(character);
            AddToUnlockedCharacters(character);
            RefreshAnimatorLookup();
            inactiveParty.RestoreCharacterState(ref character); // Restore character stats, exp, equipment, inventory (if previously in party)
            inactiveParty.RemoveFromInactiveStorage(character); // Stop tracking in inactive storage (i.e. since in active party)

            if (party.Count > 1) { character.GetComponent<Collider2D>().isTrigger = true; }
            partyUpdated?.Invoke();
            return true;
        }

        // AddToParty -- Derivative
        public bool AddToParty(CharacterNPCSwapper characterNPCSwapper)
        {
            // For direct interaction with world NPCs -> characters
            if (party.Count >= partyLimit) { return false; }
            if (characterNPCSwapper == null) { return false; } // Failsafe

            CharacterNPCSwapper partyCharacter = characterNPCSwapper.SwapToCharacter(partyContainer);
            UpdateWorldLookup(false, partyCharacter);
            Destroy(characterNPCSwapper.gameObject);

            return AddToParty(partyCharacter.GetCombatParticipant());
        }

        // AddToParty -- Derivative
        public bool AddToParty(CharacterProperties characterProperties)
        {
            // For instantiation through other means (i.e. no character exists on screen)
            if (party.Count >= partyLimit) { return false; }
            if (characterProperties == null) { return false; } // Failsafe

            GameObject characterObject = CharacterNPCSwapper.SpawnCharacter(characterProperties.name, partyContainer);

            CombatParticipant character = characterObject.GetComponent<CombatParticipant>();
            return AddToParty(character);
        }

        // RemoveFromParty -- Parent
        public bool RemoveFromParty(CombatParticipant character)
        {
            if (party.Count <= 1) { return false; }
            if (character == null) { return false; } // Failsafe

            // If character to remove is leader, swap leadership to second character first
            if (IsPartyLeader(character))
            {
                SetPartyLeader(party[1]); // Guaranteed to exist because count > 1
            }

            inactiveParty.CaptureCharacterState(character);
            party.Remove(character);
            animatorLookup.Remove(character);

            Destroy(character.gameObject);
            partyUpdated?.Invoke();

            return true;
        }

        // RemoveFromParty -- Derivative
        public bool RemoveFromParty(CombatParticipant character, Transform worldTransform)
        {
            if (party.Count <= 1) { return false; }
            if (character == null) { return false; } // Failsafe

            // Instantiates an NPC at defined location
            CharacterNPCSwapper partyCharacter = character.GetComponent<CharacterNPCSwapper>();
            if (partyCharacter == null) { return false; }

            CharacterNPCSwapper worldNPC = partyCharacter.SwapToNPC(worldTransform);
            UpdateWorldLookup(true, worldNPC);

            return RemoveFromParty(character);
        }

        public void UpdateLeaderAnimation(float speed, float xLookDirection, float yLookDirection)
        {
            CombatParticipant character = party[0];
            animatorLookup[character].SetFloat("Speed", speed);
            animatorLookup[character].SetFloat("xLook", xLookDirection);
            animatorLookup[character].SetFloat("yLook", yLookDirection);
            UpdatePartySpeed(speed);
        }
        #endregion

        #region PrivateMethods
        private void InitializeUnlockedCharacters()
        {
            foreach (CombatParticipant combatParticipant in party)
            {
                AddToUnlockedCharacters(combatParticipant);
            }
        }

        private void AddToUnlockedCharacters(CombatParticipant combatParticipant)
        {
            CharacterProperties characterProperties = combatParticipant.GetBaseStats().GetCharacterProperties();
            AddToUnlockedCharacters(characterProperties);
        }

        private void AddToUnlockedCharacters(CharacterProperties characterProperties)
        {
            if (!unlockedCharacters.Contains(characterProperties))
            {
                unlockedCharacters.Add(characterProperties);
            }
        }

        private void RemoveFromUnlockedCharacters(CombatParticipant combatParticipant)
        {
            CharacterProperties characterProperties = combatParticipant.GetBaseStats().GetCharacterProperties();
            unlockedCharacters.Remove(characterProperties);
        }

        private void RefreshAnimatorLookup()
        {
            animatorLookup.Clear();
            foreach (CombatParticipant character in party)
            {
                animatorLookup.Add(character, character.GetComponent<Animator>());
            }
        }

        private void UpdateWorldLookup(bool addToLookUp, CharacterNPCSwapper characterNPCSwapper)
        {
            string characterName = characterNPCSwapper.GetBaseStats().GetCharacterProperties().name;
            if (addToLookUp)
            {
                string sceneReference = SceneManager.GetActiveScene().name;
                Tuple<string, string> sceneParentPair = new Tuple<string, string>(sceneReference, characterNPCSwapper.transform.parent.gameObject.name);
                worldNPCLookup[characterName] = sceneParentPair;
            }
            else
            {
                if (worldNPCLookup.ContainsKey(characterName))
                {
                    worldNPCLookup.Remove(characterName);
                }
            }
        }

        private void UpdatePartySpeed(float speed)
        {
            int characterIndex = 0;
            foreach (CombatParticipant character in party)
            {
                if (characterIndex == 0) { characterIndex++; continue; }
                animatorLookup[character].SetFloat("Speed", speed);
                characterIndex++;
            }
        }

        private void UpdatePartyOffsets(CircularBuffer<Tuple<Vector2, Vector2>> movementHistory)
        {
            Vector2 leaderPosition = movementHistory.GetFirstEntry().Item1;

            int characterIndex = 0;
            foreach (CombatParticipant character in party)
            {
                if (characterIndex == 0) { characterIndex++; continue; }

                Vector2 localPosition = Vector2.zero;
                Vector2 lookDirection = Vector2.zero;
                if (characterIndex*partyOffset >= movementHistory.GetCurrentSize())
                {
                    localPosition = movementHistory.GetLastEntry().Item1 - leaderPosition;
                    lookDirection = movementHistory.GetLastEntry().Item2;
                }
                else
                {
                    localPosition = movementHistory.GetEntryAtPosition(characterIndex * partyOffset).Item1 - leaderPosition;
                    lookDirection = movementHistory.GetEntryAtPosition(characterIndex * partyOffset).Item2;
                }
                character.gameObject.transform.localPosition = localPosition;
                animatorLookup[character].SetFloat("xLook", lookDirection.x);
                animatorLookup[character].SetFloat("yLook", lookDirection.y);

                characterIndex++;
            }
        }

        private void ResetPartyOffsets()
        {
            foreach (CombatParticipant character in party)
            {
                character.gameObject.transform.localPosition = Vector2.zero;
            }
        }
        #endregion

        #region Interfaces
        [System.Serializable]
        struct PartySaveData
        {
            public List<string> partyStrings;
            public List<string> unlockedCharacterStrings;
            public Dictionary<string, Tuple<string, string>> worldNPCLookup;

            public PartySaveData(List<string> currentParty, List<string> unlockedCharacterStrings, Dictionary<string, Tuple<string, string>> worldNPCLookup)
            {
                this.partyStrings = currentParty;
                this.unlockedCharacterStrings = unlockedCharacterStrings;
                this.worldNPCLookup = worldNPCLookup;
            }
        }

        public LoadPriority GetLoadPriority()
        {
            return LoadPriority.ObjectInstantiation;
        }
        public SaveState CaptureState()
        {
            List<string> currentPartyStrings = new List<string>();
            foreach (CombatParticipant combatParticipant in party)
            {
                currentPartyStrings.Add(combatParticipant.GetBaseStats().GetCharacterProperties().name);
            }
            List<string> unlockedCharacterStrings = new List<string>();
            foreach (CharacterProperties characterProperties in unlockedCharacters)
            {
                unlockedCharacterStrings.Add(characterProperties.name);
            }

            PartySaveData partySaveState = new PartySaveData(currentPartyStrings, unlockedCharacterStrings, worldNPCLookup);
            SaveState saveState = new SaveState(GetLoadPriority(), partySaveState);
            return saveState;
        }

        public void RestoreState(SaveState saveState)
        {
            PartySaveData data = (PartySaveData)saveState.GetState();
            // Clear characters in existing party in scene
            foreach (CombatParticipant combatParticipant in party)
            {
                Destroy(combatParticipant.gameObject);
            }
            party.Clear();

            // Pull characters from save
            List<string> addPartyStrings = data.partyStrings;
            if (addPartyStrings != null)
            {
                foreach (string characterName in addPartyStrings)
                {
                    if (party.Count > partyLimit) { break; } // Failsafe

                    GameObject character = CharacterNPCSwapper.SpawnCharacter(characterName, partyContainer);
                    if (character == null) { return; }

                    CombatParticipant combatParticipant = character.GetComponent<CombatParticipant>();
                    if (combatParticipant == null) { Destroy(character); return; }

                    party.Add(combatParticipant);

                    if (party.Count > 1) { character.GetComponent<Collider2D>().isTrigger = true; }
                }
                RefreshAnimatorLookup();
            }
            partyUpdated?.Invoke();

            // Build up unlocked characters list
            List<string> unlockedCharacterStrings = data.unlockedCharacterStrings;
            if (unlockedCharacterStrings != null)
            {
                foreach (string characterName in unlockedCharacterStrings)
                {
                    CharacterProperties characterProperties = CharacterProperties.GetCharacterPropertiesFromName(characterName);
                    AddToUnlockedCharacters(characterProperties);
                }
            }

            // Instantiate world NPCs
            worldNPCLookup = data.worldNPCLookup;
            if (worldNPCLookup != null)
            {
                foreach (KeyValuePair<string, Tuple<string, string>> worldNPCEntry in worldNPCLookup)
                {
                    if (worldNPCEntry.Value.Item1 == SceneManager.GetActiveScene().name)
                    {
                        GameObject parent = GameObject.Find(worldNPCEntry.Value.Item2);
                        if (parent == null) { return; }

                        CharacterNPCSwapper.SpawnNPC(worldNPCEntry.Key, parent.transform);
                    }
                }
            }
        }

        public bool? Evaluate(Predicate predicate)
        {
            PredicateParty predicateParty = predicate as PredicateParty;
            return predicateParty != null ? predicateParty.Evaluate(this) : null;
        }
        #endregion
    }
}
