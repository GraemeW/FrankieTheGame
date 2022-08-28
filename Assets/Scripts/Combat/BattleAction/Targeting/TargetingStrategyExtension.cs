using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Combat
{
    public static class TargetingStrategyExtension
    {
        public static IEnumerable<BattleEntity> GetCombatParticipantsByType(this TargetingStrategy targetingStrategy, CombatParticipantType combatParticipantType, IEnumerable<BattleEntity> activeCharacters, IEnumerable<BattleEntity> activeEnemies)
        {
            if (combatParticipantType == CombatParticipantType.Either || combatParticipantType == CombatParticipantType.Foe)
            {
                if (activeEnemies != null)
                {
                    foreach (BattleEntity enemy in activeEnemies)
                    {
                        yield return enemy;
                    }
                }
            }
            if (combatParticipantType == CombatParticipantType.Either || combatParticipantType == CombatParticipantType.Friendly)
            {
                if (activeCharacters != null)
                {
                    foreach (BattleEntity character in activeCharacters)
                    {
                        yield return character;
                    }
                }
            }
        }
    }
}