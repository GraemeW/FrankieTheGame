using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Combat
{
    [CreateAssetMenu(fileName = "New Self Targeting", menuName = "BattleAction/Targeting/Self Target")]
    public class SelfTargeting : TargetingStrategy
    {
        public override void SetTargets(TargetingNavigationType targetingNavigationType, BattleActionData battleActionData,
            IEnumerable<BattleEntity> activeCharacters, IEnumerable<BattleEntity> activeEnemies)
        {
            // Separate out overall set & filter
            battleActionData.SetTargets(GetCombatParticipantsByType(combatParticipantType, activeCharacters, activeEnemies));
            FilterTargets(battleActionData, filterStrategies);
            if (!battleActionData.HasTargets()) { return; }

            // Set Focal Target
            battleActionData.SetFocalTargetToSender();
            
            // Assign Focal Target to Target
            battleActionData.SetTargetFromFocalTarget();
        }
    }
}
