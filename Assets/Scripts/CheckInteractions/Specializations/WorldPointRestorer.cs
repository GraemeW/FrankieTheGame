using Frankie.Combat;
using Frankie.Stats;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Control.Specialization
{
    public class WorldPointRestorer : MonoBehaviour
    {
        public void ReviveAndHealParty(PlayerStateMachine playerStateHandler) // Called via Unity events
        {
            PartyCombatConduit partyCombatConduit = playerStateHandler.GetComponent<PartyCombatConduit>();
            foreach (CombatParticipant combatParticipant in partyCombatConduit.GetPartyCombatParticipants())
            {
                combatParticipant.Revive(combatParticipant.GetBaseStats().GetStat(Stat.HP));
            }
        }

        public void RestorePartyAP(PlayerStateMachine playerStateHandler) // Called via Unity Events
        {
            PartyCombatConduit partyCombatConduit = playerStateHandler.GetComponent<PartyCombatConduit>();
            foreach (CombatParticipant combatParticipant in partyCombatConduit.GetPartyCombatParticipants())
            {
                combatParticipant.AdjustAP(combatParticipant.GetBaseStats().GetStat(Stat.AP));
            }
        }

        public void ReviveAndHealAttachedCharacter(PlayerStateMachine playerStateHandler) // Called via Unity Events
        {
            if (!gameObject.TryGetComponent(out CombatParticipant combatParticipant)) { return; }

            combatParticipant.Revive(combatParticipant.GetBaseStats().GetStat(Stat.HP));
        }
    }
}