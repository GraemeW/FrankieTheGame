using UnityEngine;
using Frankie.Combat;
using Frankie.Stats;

namespace Frankie.Control.Specialization
{
    public class WorldPointAdjuster : MonoBehaviour
    {
        // Tunables
        [SerializeField][Tooltip("Only used if calling methods that consume points")] float hpToAdjust;
        [SerializeField][Tooltip("Only used if calling methods that consume points")] float apToAdjust;

        // Public Methods
        public void AdjustPartyLeaderHP(PlayerStateMachine playerStateMachine) // Called via Unity events
        {
            var partyCombatConduit = playerStateMachine.GetComponent<PartyCombatConduit>();
            CombatParticipant partyLeader = partyCombatConduit.GetPartyLeader();
            if (partyLeader == null) return;
            partyLeader.AdjustHP(hpToAdjust);
            partyLeader.CheckIfDead();
        }

        public void AdjustPartyLeaderAP(PlayerStateMachine playerStateMachine) // Called via Unity events
        {
            var partyCombatConduit = playerStateMachine.GetComponent<PartyCombatConduit>();
            CombatParticipant partyLeader = partyCombatConduit.GetPartyLeader();
            if (partyLeader == null) return;
            partyLeader.AdjustHP(apToAdjust);
        }

        public void AdjustPartyHP(PlayerStateMachine playerStateMachine) // Called via Unity events
        {
            var partyCombatConduit = playerStateMachine.GetComponent<PartyCombatConduit>();
            foreach (CombatParticipant combatParticipant in partyCombatConduit.GetPartyCombatParticipants())
            {
                combatParticipant.AdjustHP(hpToAdjust);
                combatParticipant.CheckIfDead();
            }
        }

        public void AdjustPartyAP(PlayerStateMachine playerStateMachine) // Called via Unity events
        {
            var partyCombatConduit = playerStateMachine.GetComponent<PartyCombatConduit>();
            foreach (CombatParticipant combatParticipant in partyCombatConduit.GetPartyCombatParticipants())
            {
                combatParticipant.AdjustAP(apToAdjust);
            }
        }

        public void ReviveAndHealParty(PlayerStateMachine playerStateHandler) // Called via Unity events
        {
            var partyCombatConduit = playerStateHandler.GetComponent<PartyCombatConduit>();
            foreach (CombatParticipant combatParticipant in partyCombatConduit.GetPartyCombatParticipants())
            {
                if (combatParticipant.IsDead()) { combatParticipant.Revive(combatParticipant.GetMaxHP()); }
                else { combatParticipant.AdjustHP( combatParticipant.GetMaxHP() + Mathf.Epsilon ); }
            }
        }

        public void RestorePartyAP(PlayerStateMachine playerStateHandler) // Called via Unity Events
        {
            var partyCombatConduit = playerStateHandler.GetComponent<PartyCombatConduit>();
            foreach (CombatParticipant combatParticipant in partyCombatConduit.GetPartyCombatParticipants())
            {
                combatParticipant.AdjustAP(combatParticipant.GetMaxAP());
            }
        }

        public void ReviveAndHealAttachedCharacter(PlayerStateMachine playerStateHandler) // Called via Unity Events
        {
            if (!gameObject.TryGetComponent(out CombatParticipant combatParticipant)) { return; }

            if (combatParticipant.IsDead()) { combatParticipant.Revive(combatParticipant.GetMaxHP() + 1f); }
            else { combatParticipant.AdjustHP( combatParticipant.GetMaxHP() + 1f); }
        }
    }
}
