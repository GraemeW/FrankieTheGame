using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frankie.Combat;
using Cinemachine;
using Frankie.Control;
using Frankie.Utils;
using System;

namespace Frankie.Stats
{
    public class Party : MonoBehaviour
    {
        // Tunables
        [SerializeField][Range(1,4)] int partyLimit = 4;
        [SerializeField] List<CombatParticipant> party = new List<CombatParticipant>();
        [SerializeField] Transform partyContainer = null;
        [SerializeField] int partyOffset = 16;

        // State
        Dictionary<CombatParticipant, Animator> animatorLookup = new Dictionary<CombatParticipant, Animator>();

        // Cached References
        PlayerMover playerMover = null;

        private void Awake()
        {
            playerMover = GetComponent<PlayerMover>();
            foreach (CombatParticipant character in party)
            {
                animatorLookup.Add(character, character.GetComponent<Animator>());
            }
        }

        private void OnEnable()
        {
            playerMover.movementHistoryReset += ResetPartyOffsets;
            playerMover.leaderAnimatorUpdated += UpdateLeaderAnimation;
            playerMover.playerMoved += UpdatePartyOffsets;
        }

        public void SetPartyLeader(CombatParticipant character)
        {
            // TODO:  Implement, call event to update camera controller
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
            Destroy(worldNPC.gameObject);

            party.Add(partyCharacter.GetCombatParticipant());
            animatorLookup.Add(partyCharacter.GetCombatParticipant(), partyCharacter.GetComponent<Animator>());
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
            else if (party.Count == 2)
            {
                nextMember = party[1];
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
    }
}
