using UnityEngine;
using Frankie.Combat;
using Frankie.Stats;

namespace Frankie.Control.Specialization
{
    public class WorldPointAdjuster : MonoBehaviour
    {
        // Tunables
        [SerializeField][Tooltip("Only used if calling methods that consume points")] float hpToAdjust = 0;
        [SerializeField][Tooltip("Only used if calling methods that consume points")] float apToAdjust = 0;

        // Public Methods
        public void AdjustPartyLeaderHP(PlayerStateMachine playerStateMachine) // Called via Unity events
        {
            PartyCombatConduit partyCombatConduit = playerStateMachine.GetComponent<PartyCombatConduit>();
            CombatParticipant partyLeader = partyCombatConduit.GetPartyLeader();
            if (partyLeader != null)
            {
                partyLeader.AdjustHP(hpToAdjust);
                partyLeader.CheckIfDead();
            }
        }

        public void AdjustPartyLeaderAP(PlayerStateMachine playerStateMachine) // Called via Unity events
        {
            PartyCombatConduit partyCombatConduit = playerStateMachine.GetComponent<PartyCombatConduit>();
            CombatParticipant partyLeader = partyCombatConduit.GetPartyLeader();
            if (partyLeader != null)
            {
                partyLeader.AdjustHP(apToAdjust);
            }
        }

        public void AdjustPartyHP(PlayerStateMachine playerStateMachine) // Called via Unity events
        {
            PartyCombatConduit partyCombatConduit = playerStateMachine.GetComponent<PartyCombatConduit>();
            foreach (CombatParticipant combatParticipant in partyCombatConduit.GetPartyCombatParticipants())
            {
                combatParticipant.AdjustHP(hpToAdjust);
                combatParticipant.CheckIfDead();
            }
        }

        public void AdjustPartyAP(PlayerStateMachine playerStateMachine) // Called via Unity events
        {
            PartyCombatConduit partyCombatConduit = playerStateMachine.GetComponent<PartyCombatConduit>();
            foreach (CombatParticipant combatParticipant in partyCombatConduit.GetPartyCombatParticipants())
            {
                combatParticipant.AdjustAP(apToAdjust);
            }
        }

        public void ReviveAndHealParty(PlayerStateMachine playerStateHandler) // Called via Unity events
        {
            PartyCombatConduit partyCombatConduit = playerStateHandler.GetComponent<PartyCombatConduit>();
            foreach (CombatParticipant combatParticipant in partyCombatConduit.GetPartyCombatParticipants())
            {
                combatParticipant.Revive(combatParticipant.GetMaxHP());
            }
        }

        public void RestorePartyAP(PlayerStateMachine playerStateHandler) // Called via Unity Events
        {
            PartyCombatConduit partyCombatConduit = playerStateHandler.GetComponent<PartyCombatConduit>();
            foreach (CombatParticipant combatParticipant in partyCombatConduit.GetPartyCombatParticipants())
            {
                combatParticipant.AdjustAP(combatParticipant.GetMaxAP());
            }
        }

        public void ReviveAndHealAttachedCharacter(PlayerStateMachine playerStateHandler) // Called via Unity Events
        {
            if (!gameObject.TryGetComponent(out CombatParticipant combatParticipant)) { return; }

            combatParticipant.Revive(combatParticipant.GetMaxHP());
        }
    }
}
