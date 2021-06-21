using Frankie.Combat;
using Frankie.Stats;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Control.Specialization
{
    public class WorldHealer : MonoBehaviour
    {
        public void HealParty(PlayerStateHandler playerStateHandler) // Called via Unity events
        {
            foreach (CombatParticipant combatParticipant in playerStateHandler.GetParty().GetParty())
            {
                combatParticipant.Revive(combatParticipant.GetBaseStats().GetBaseStat(Stat.HP));
            }
        }
    }
}