using Frankie.Combat;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Stats
{
    [RequireComponent(typeof(Party))]
    public class PartyCombatConduit : MonoBehaviour
    {
        // State
        List<CombatParticipant> combatParticipantCache = new List<CombatParticipant>();

        // Cached References
        Party party = null;

        #region UnityMethods
        private void Awake()
        {
            party = GetComponent<Party>();
        }

        private void OnEnable()
        {
            party.partyUpdated += RefreshCombatParticipantCache;
            RefreshCombatParticipantCache();
        }

        private void OnDisable()
        {
            party.partyUpdated -= RefreshCombatParticipantCache;
        }
        #endregion

        #region PublicMethods
        public List<CombatParticipant> GetPartyCombatParticipants() => combatParticipantCache;

        public string GetPartyLeaderName() => party.GetPartyLeaderName();

        public bool IsAnyMemberAlive()
        {
            bool alive = false;
            foreach (CombatParticipant combatParticipant in combatParticipantCache)
            {
                if (!combatParticipant.IsDead()) { alive = true; }
            }
            return alive;
        }
        #endregion

        #region PrivateMethods
        private void RefreshCombatParticipantCache()
        {
            combatParticipantCache.Clear();
            foreach (BaseStats character in party.GetParty())
            {
                if (character.TryGetComponent(out CombatParticipant combatParticipant))
                {
                    combatParticipantCache.Add(combatParticipant);
                }
            }
        }
        #endregion
    }
}