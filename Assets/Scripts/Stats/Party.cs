using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frankie.Combat;
using Cinemachine;

namespace Frankie.Stats
{
    public class Party : MonoBehaviour
    {
        // Tunables
        [SerializeField][Range(1,4)] int partyLimit = 4;
        [SerializeField] List<CombatParticipant> party = new List<CombatParticipant>();

        // State
        Dictionary<CombatParticipant, Animator> animatorLookup = new Dictionary<CombatParticipant, Animator>();

        private void Awake()
        {
            foreach (CombatParticipant character in party)
            {
                animatorLookup.Add(character, character.GetComponent<Animator>());
            }
        }

        public void SetPartyLeader(CombatParticipant character)
        {
            // TODO:  Implement, call event to update camera controller
        }

        public Animator GetLeadCharacterAnimator()
        {
            return animatorLookup[party[0]];
        }

        public bool AddToParty(CombatParticipant character)
        {
            // TODO:  Add GameObject Instatiation && Transform offsetting for multiple characters
            if (party.Count >= partyLimit) { return false; }
            party.Add(character);
            animatorLookup.Add(character, character.GetComponent<Animator>());
            return true;
        }

        public bool RemoveFromParty(CombatParticipant character)
        {
            if (party.Count <= 1) { return false; }
            party.Remove(character);
            animatorLookup.Remove(character);
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

        public void UpdatePartyAnimation(float speed, float xLookDirection, float yLookDirection)
        {
            foreach (CombatParticipant character in party)
            {
                animatorLookup[character].SetFloat("Speed", speed);
                animatorLookup[character].SetFloat("xLook", xLookDirection);
                animatorLookup[character].SetFloat("yLook", yLookDirection);
            }
        }
    }
}
