using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Frankie.Combat;
using Frankie.ZoneManagement;

namespace Frankie.Core.PlayerStateMemory
{
    public class CombatMemory
    {
        public bool combatFadeComplete = false;
        public readonly List<CombatParticipant> enemiesUnderConsideration = new();
        public readonly List<CombatParticipant> enemiesInTransition = new();
        public GameObject battleUIInstance;
        
        public bool AreCombatParticipantsValid() => !enemiesUnderConsideration.All(x => x.IsDead());

        public void ShiftEnemiesFromConsiderationToTransition(int maxEnemiesPerCombat)
        {
            foreach (CombatParticipant enemy in enemiesUnderConsideration)
            {
                if (enemiesInTransition.Count > maxEnemiesPerCombat) { return; }
                if (!enemiesInTransition.Contains(enemy)) { enemiesInTransition.Add(enemy); }
            }
        }

        public bool BeginFade(TransitionType currentTransitionType, FaderEventTriggers<TransitionType> faderEventTriggers)
        {
            combatFadeComplete = false;
            bool faderInitiated = Fader.StartStandardFade(currentTransitionType, faderEventTriggers);
            if (!faderInitiated) { combatFadeComplete = true; }
            return faderInitiated;
        }

        public bool ConcludeFade(TransitionType currentTransitionType, FaderEventTriggers<TransitionType> faderEventTriggers) => Fader.StartStandardFade(currentTransitionType, faderEventTriggers);
    }
}
