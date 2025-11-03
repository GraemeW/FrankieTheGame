using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Combat
{
    [CreateAssetMenu(fileName = "New Single Targeting", menuName = "BattleAction/Targeting/Single Target")]
    public class SingleTargeting : TargetingStrategy
    {
        public override void SetTargets(TargetingNavigationType targetingNavigationType, BattleActionData battleActionData,
            IEnumerable<BattleEntity> activeCharacters, IEnumerable<BattleEntity> activeEnemies)
        {
            // Separate out overall set & filter
            battleActionData.SetTargets(GetCombatParticipantsByType(combatParticipantType, activeCharacters, activeEnemies));
            FilterTargets(battleActionData, filterStrategies);
            if (!battleActionData.HasTargets()) { return; }

            // Set Focal Target
            battleActionData.SetFocalTarget(targetingNavigationType);
            
            // Assign Focal Target to Target
            battleActionData.SetTargetFromFocalTarget();
        }
    }
}
