using Frankie.Combat;
using Frankie.Stats;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Control.Specialization
{
    public class WorldPointRestorer : MonoBehaviour
    {
        public void ReviveAndHealParty(PlayerStateHandler playerStateHandler) // Called via Unity events
        {
            foreach (CombatParticipant combatParticipant in playerStateHandler.GetParty().GetParty())
            {
                combatParticipant.Revive(combatParticipant.GetBaseStats().GetStat(Stat.HP));
            }
        }

        public void RestorePartyAP(PlayerStateHandler playerStateHandler) // Called via Unity Events
        {
            foreach (CombatParticipant combatParticipant in playerStateHandler.GetParty().GetParty())
            {
                combatParticipant.AdjustAP(combatParticipant.GetBaseStats().GetStat(Stat.AP));
            }
        }

        public void ReviveAndHealAttachedCharacter(PlayerStateHandler playerStateHandler) // Called via Unity Events
        {
            if (!gameObject.TryGetComponent(out CombatParticipant combatParticipant)) { return; }

            combatParticipant.Revive(combatParticipant.GetBaseStats().GetStat(Stat.HP));
        }
    }
}