using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frankie.Combat;

namespace Frankie.Stats
{
    public class Party : MonoBehaviour
    {
        // Tunables
        [SerializeField][Range(1,4)] int partyLimit = 4;
        [SerializeField] Transform partyContainer = null;
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