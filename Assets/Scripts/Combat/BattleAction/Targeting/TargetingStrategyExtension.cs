using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Combat
{
    public static class TargetingStrategyExtension
    {
        public static List<CombatParticipant> GetCombatParticipantsByType(this TargetingStrategy targetingStrategy, CombatParticipantType combatParticipantType, IEnumerable<CombatParticipant> activeCharacters, IEnumerable<CombatParticipant> activeEnemies)
        {
            List<CombatParticipant> potentialTargets = new List<CombatParticipant>();
            if (combatParticipantType == CombatParticipantType.Either || combatParticipantType == CombatParticipantType.Target)
            {
                if (activeEnemies != null) { potentialTargets.AddRange(activeEnemies); }
            }
            if (combatParticipantType == CombatParticipantType.Either || combatParticipantType == CombatParticipantType.Character)
            {
                if (activeCharacters != null) { potentialTargets.AddRange(activeCharacters); }
            }
            return potentialTargets;
        }
    }
}