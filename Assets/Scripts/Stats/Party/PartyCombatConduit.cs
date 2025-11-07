using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frankie.Combat;

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
        public CombatParticipant GetPartyLeader() => combatParticipantCache[0];
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
                fearsomeStat = Mathf.Max(fearsomeStat, newFearsomeStat);
            }

            return fearsomeStat > 0f;
        }

        public bool IsImposing(CombatParticipant toEnemy)
        {
            float imposingStat = -1f;
            foreach (CombatParticipant character in combatParticipantCache)
            {
                float newImposingStat = character.GetCalculatedStat(CalculatedStat.Imposing, toEnemy);
                imposingStat = Mathf.Max(imposingStat, newImposingStat);
            }

            return imposingStat > 0f;
        }
        #endregion

        #region PrivateMethods
        private void HandleLeaderStatusUpdate(StateAlteredInfo stateAlteredInfo)
        {
            if (stateAlteredInfo.stateAlteredType != StateAlteredType.Dead) { return; }

            foreach (CombatParticipant character in combatParticipantCache)
            {
                if (character.IsDead()) { continue; }
                BaseStats baseStats = character.GetComponent<BaseStats>();
                party.SetPartyLeader(baseStats);
                break;
            }
        }

        private void SubscribeToLeaderStatusUpdates(bool enable)
        {
            if (combatParticipantCache.Count == 0) { return; }
            CombatParticipant partyLeader = combatParticipantCache[0];
            if (partyLeader == null) { return; }

            if (enable)
            {
                partyLeader.SubscribeToStateUpdates(HandleLeaderStatusUpdate);
            }
            else
            {
                partyLeader.UnsubscribeToStateUpdates(HandleLeaderStatusUpdate);
            }
        }

        private void RefreshCombatParticipantCache()
        {
            SubscribeToLeaderStatusUpdates(false);
            combatParticipantCache.Clear();
            foreach (BaseStats character in party.GetParty())
            {
                if (character.TryGetComponent(out CombatParticipant combatParticipant))
                {
                    combatParticipantCache.Add(combatParticipant);
                }
            }
            SubscribeToLeaderStatusUpdates(true);
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
