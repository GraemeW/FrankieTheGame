using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Combat
{
    public static class TargetingStrategyExtension
    {
        public static IEnumerable<CombatParticipant> GetCombatParticipantsByType(this TargetingStrategy targetingStrategy, CombatParticipantType combatParticipantType, IEnumerable<CombatParticipant> activeCharacters, IEnumerable<CombatParticipant> activeEnemies)
        {
            if (combatParticipantType == CombatParticipantType.Either || combatParticipantType == CombatParticipantType.Foe)
            {
                if (activeEnemies != null)
                {
                    foreach (CombatParticipant enemy in activeEnemies)
                    {
                        yield return enemy;
                    }
                }
            }
            if (combatParticipantType == CombatParticipantType.Either || combatParticipantType == CombatParticipantType.Friendly)
            {
                if (activeCharacters != null)
                {
                    foreach (CombatParticipant character in activeCharacters)
                    {
                        yield return character;
                    }
                }
            }
        }
    }
}