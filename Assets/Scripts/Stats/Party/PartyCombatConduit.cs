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
        string partyLeaderName = string.Empty;
        private readonly List<CombatParticipant> combatParticipantCache = new();
        private readonly List<CombatParticipant> combatAssistCache = new();

        #region UnityMethods
        private void OnEnable()
        {
            if (TryGetComponent(out Party party))
            {
                party.SubscribeToMembersAlteredUpdates(true, RefreshCombatParticipantCache);
                party.SubscribeToPartyLeaderAnnouncements(true, RefreshPartyLeaderCache);
                RefreshCombatParticipantCache(party.GetMembers());
            }
            if (TryGetComponent(out PartyAssist partyAssist))
            {
                partyAssist.SubscribeToMembersAlteredUpdates(true, RefreshCombatAssistCache);
                RefreshCombatAssistCache(partyAssist.GetMembers());
            }
        }

        private void OnDisable()
        {
            if (TryGetComponent(out Party party))
            {
                party.SubscribeToMembersAlteredUpdates(false, RefreshCombatParticipantCache);
                party.SubscribeToPartyLeaderAnnouncements(false, RefreshPartyLeaderCache);
            }
            if (TryGetComponent(out PartyAssist partyAssist)) { partyAssist.SubscribeToMembersAlteredUpdates(true, RefreshCombatAssistCache); }
        }
        #endregion

        #region PublicMethods
        public CombatParticipant GetPartyLeader() => combatParticipantCache[0];
        public List<CombatParticipant> GetPartyCombatParticipants() => combatParticipantCache;
        public List<CombatParticipant> GetPartyAssistParticipants() => combatAssistCache;

        public string GetPartyLeaderName() => partyLeaderName;
        public bool IsPartySolo() => combatParticipantCache.Count == 1;

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
                if (character.IsDead()) { continue; }
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
                if (character.IsDead()) { continue; }
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
                var baseStats = character.GetComponent<BaseStats>();
                baseStats.TrySetToPartyLeader();
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

        private void RefreshCombatParticipantCache(List<BaseStats> members)
        {
            members ??= new List<BaseStats>();
            SubscribeToLeaderStatusUpdates(false);
            combatParticipantCache.Clear();
            foreach (BaseStats character in members)
            {
                if (character.TryGetComponent(out CombatParticipant combatParticipant))
                {
                    combatParticipantCache.Add(combatParticipant);
                }
            }
            SubscribeToLeaderStatusUpdates(true);
        }

        private void RefreshPartyLeaderCache(string newPartyLeaderName, Animator _)
        {
            partyLeaderName = newPartyLeaderName;
        }

        private void RefreshCombatAssistCache(List<BaseStats> members)
        {
            members ??= new List<BaseStats>();
            
            combatAssistCache.Clear();
            foreach (BaseStats character in members)
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
