using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Frankie.Combat
{
    [CreateAssetMenu(fileName = "New All Targeting", menuName = "BattleAction/Targeting/Target All")]
    public class TargetAll : TargetingStrategy
    {

        public override void SetTargets(TargetingNavigationType targetingNavigationType, BattleActionData battleActionData,
            IEnumerable<BattleEntity> activeCharacters, IEnumerable<BattleEntity> activeEnemies)
        {
            // Separate out overall set & filter
            battleActionData.SetTargets(GetCombatParticipantsByType(combatParticipantType, activeCharacters, activeEnemies));
            FilterTargets(battleActionData, filterStrategies);
            
            // Done -- Pass back whole set
        }
    }
}
