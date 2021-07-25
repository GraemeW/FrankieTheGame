using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frankie.Combat;
using Cinemachine;
using Frankie.Control;
using Frankie.Utils;
using System;
using Frankie.Saving;
using UnityEngine.SceneManagement;

namespace Frankie.Stats
{
    public class Party : MonoBehaviour, ISaveable
    {
        // Tunables
        [SerializeField][Range(1,4)] int partyLimit = 4;
        [SerializeField] List<CombatParticipant> party = new List<CombatParticipant>();
        [SerializeField] Transform partyContainer = null;
        [SerializeField] int partyOffset = 16;

        // State
        Dictionary<string, Tuple<string, string>> worldNPCLookup = new Dictionary<string, Tuple<string, string>>();
        Dictionary<CombatParticipant, Animator> animatorLookup = new Dictionary<CombatParticipant, Animator>();

        // Cached References
        PlayerMover playerMover = null;

        private void Awake()
        {
            playerMover = GetComponent<PlayerMover>();
            RefreshAnimatorLookup();
        }

        private void OnEnable()
        {
            playerMover.movementHistoryReset += ResetPartyOffsets;
            playerMover.leaderAnimatorUpdated += UpdateLeaderAnimation;
            playerMover.playerMoved += UpdatePartyOffsets;
        }

        private void RefreshAnimatorLookup()
        {
            animatorLookup.Clear();
            foreach (CombatParticipant character in party)
            {
                animatorLookup.Add(character, character.GetComponent<Animator>());
            }
        }

        public void SetPartyLeader(CombatParticipant character)
        {
            // TODO:  Implement, call event to update camera controller
            // TODO:  update the layers (i.e. put the new leader onto party leader layer;  put old leader onto other characters layer)
        }

        public CombatParticipant GetPartyLeader()
        {
            return party[0];
        }

        public Animator GetLeadCharacterAnimator()
        {
            return animatorLookup[party[0]];
        }

        public bool AddToParty(CombatParticipant character)
        {
            if (party.Count >= partyLimit) { return false; }

            CharacterNPCSwapper worldNPC = character.GetComponent<CharacterNPCSwapper>();
            if (worldNPC == null) { return false; }

            CharacterNPCSwapper partyCharacter = worldNPC.SwapToCharacter(partyContainer);
            UpdateWorldLookup(false, partyCharacter);
            Destroy(worldNPC.gameObject);

            party.Add(partyCharacter.GetCombatParticipant());
            RefreshAnimatorLookup();
            return true;
        }

        public bool RemoveFromParty(CombatParticipant character, Transform worldTransform)
        {
            if (party.Count <= 1) { return false; }
            party.Remove(character);
            animatorLookup.Remove(character);

            CharacterNPCSwapper partyCharacter = character.GetComponent<CharacterNPCSwapper>();
            if (partyCharacter == null) { return false; }

            CharacterNPCSwapper worldNPC = partyCharacter.SwapToNPC(worldTransform);
            UpdateWorldLookup(true, worldNPC);
            Destroy(partyCharacter.gameObject);

            return true;
        }

        public void OverrideParty(List<CombatParticipant> party)
        {
            this.party = party;
        }

        public List<CombatParticipant> GetParty()
        {
            return party;
        }

        public bool HasMember(CombatParticipant member)
        {
            foreach (CombatParticipant combatParticipant in party)
            {
                if (combatParticipant == member)
                {
                    return true;
                }
            }
            return false;
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

        public bool IsAnyMemberAlive()
        {
            bool alive = false;
            foreach (CombatParticipant combatParticipant in party)
            {
                if (!combatParticipant.IsDead()) { alive = true; }
            }
            return alive;
        }

        public void UpdateLeaderAnimation(float speed, float xLookDirection, float yLookDirection)
        {
            CombatParticipant character = party[0];
            animatorLookup[character].SetFloat("Speed", speed);
            animatorLookup[character].SetFloat("xLook", xLookDirection);
            animatorLookup[character].SetFloat("yLook", yLookDirection);
            UpdatePartySpeed(speed);
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

        #region Interfaces
        [System.Serializable]
        struct PartySaveData
        {
            public List<string> partyStrings;
            public Dictionary<string, Tuple<string, string>> worldNPCLookup;

            public PartySaveData(List<string> currentParty, Dictionary<string, Tuple<string, string>> worldNPCLookup)
            {
                this.partyStrings = currentParty;
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

            PartySaveData partySaveState = new PartySaveData(currentPartyStrings, worldNPCLookup);
            SaveState saveState = new SaveState(GetLoadPriority(), partySaveState);
            return saveState;
        }

        public void RestoreState(SaveState saveState)
        {
            PartySaveData data = (PartySaveData)saveState.GetState();
            // Instantiate current party
            List<string> currentPartyStrings = new List<string>();
            foreach (CombatParticipant combatParticipant in party)
            {
                currentPartyStrings.Add(combatParticipant.GetBaseStats().GetCharacterProperties().name);
            }

            List<string> addPartyStrings = data.partyStrings;
            foreach(string characterName in addPartyStrings)
            {
                if (currentPartyStrings.Contains(characterName)) { continue; }

                GameObject character = CharacterNPCSwapper.SpawnCharacter(characterName, partyContainer);
                if (character == null) { return; }

                CombatParticipant combatParticipant = character.GetComponent<CombatParticipant>();
                if (combatParticipant == null) { Destroy(character); return; }

                party.Add(combatParticipant);
            }
            RefreshAnimatorLookup();

            // Instantiate world NPCs
            worldNPCLookup = data.worldNPCLookup;
            foreach(KeyValuePair<string, Tuple<string, string>> worldNPCEntry in worldNPCLookup)
            {
                if (worldNPCEntry.Value.Item1 == SceneManager.GetActiveScene().name)
                {
                    GameObject parent = GameObject.Find(worldNPCEntry.Value.Item2);
                    if (parent == null) { return; }

                    CharacterNPCSwapper.SpawnNPC(worldNPCEntry.Key, parent.transform);
                }
            }
        }
        #endregion
    }
}
