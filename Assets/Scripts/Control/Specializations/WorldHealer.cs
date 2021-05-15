using Frankie.Combat;
using Frankie.Stats;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Control
{
    public class WorldHealer : MonoBehaviour
    {
        public void HealParty(PlayerStateHandler playerStateHandler)
        {
            foreach (CombatParticipant combatParticipant in playerStateHandler.GetParty().GetParty())
            {
                combatParticipant.Revive(combatParticipant.GetBaseStats().GetBaseStat(Stat.HP));
            }
        }
    }
}