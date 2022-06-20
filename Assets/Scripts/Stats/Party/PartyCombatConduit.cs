using Frankie.Combat;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Stats
{
    [RequireComponent(typeof(Party))]
    [RequireComponent(typeof(PartyAssist))]
    public class PartyCombatConduit : MonoBehaviour
    {
        // State
        // Note:  Caching to avoid having to translate BaseStats -> CombatParticipant on every call (often)
        List<CombatParticipant> combatParticipantCache = new List<CombatParticipant>();
        List<CombatParticipant> combatAssistCache = new List<CombatParticipant>();

        // Cached References
        Party party = null;
        PartyAssist partyAssist = null;

        #region UnityMethods
        private void Awake()
        {
            party = GetComponent<Party>();
            partyAssist = GetComponent<PartyAssist>();
        }

        private void OnEnable()
        {
            party.partyUpdated += RefreshCombatParticipantCache;
            partyAssist.partyAssistUpdated += RefreshCombatAssistCache;
            RefreshCombatParticipantCache();
            RefreshCombatAssistCache();
        }

        private void OnDisable()
        {
            party.partyUpdated -= RefreshCombatParticipantCache;
            partyAssist.partyAssistUpdated -= RefreshCombatAssistCache;
        }
        #endregion

        #region PublicMethods
        public List<CombatParticipant> GetPartyCombatParticipants() => combatParticipantCache;
        public List<CombatParticipant> GetPartyAssistParticipants() => combatAssistCache;

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

        public bool IsFearsome(CombatParticipant toEnemy)
        {
            float fearsomeStat = -1f;
            foreach (CombatParticipant character in combatParticipantCache)
            {
                float newFearsomeStat = character.GetCalculatedStat(CalculatedStat.Fearsome, toEnemy);
                fearsomeStat = newFearsomeStat > fearsomeStat ? newFearsomeStat : fearsomeStat;
            }

            return fearsomeStat > 0f;
        }

        public bool IsImposing(CombatParticipant toEnemy)
        {
            float imposingStat = -1f;
            foreach (CombatParticipant character in combatParticipantCache)
            {
                float newImposingStat = character.GetCalculatedStat(CalculatedStat.Imposing, toEnemy);
                imposingStat = newImposingStat > imposingStat ? newImposingStat : imposingStat;
            }

            return imposingStat > 0f;
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

        private void RefreshCombatAssistCache()
        {
            combatAssistCache.Clear();
            foreach (BaseStats character in partyAssist.GetParty())
            {
                if (character.TryGetComponent(out CombatParticipant combatParticipant))
                {
                    combatAssistCache.Add(combatParticipant);
                }
            }
        }
        #endregion
    }
}