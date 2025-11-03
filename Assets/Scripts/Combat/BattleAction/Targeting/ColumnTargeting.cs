using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Frankie.Combat
{
    [CreateAssetMenu(fileName = "New Column Targeting", menuName = "BattleAction/Targeting/Column Target")]
    public class ColumnTargeting : TargetingStrategy
    {
        public override void SetTargets(TargetingNavigationType targetingNavigationType, BattleActionData battleActionData, IEnumerable<BattleEntity> activeCharacters, IEnumerable<BattleEntity> activeEnemies)
        {
            // Filter
            battleActionData.SetTargets(GetCombatParticipantsByType(combatParticipantType, activeCharacters, activeEnemies));
            FilterTargets(battleActionData, filterStrategies);
            if (!battleActionData.HasTargets()) { return; }

            // Set Focal Target
            battleActionData.SetFocalTarget(targetingNavigationType);
            
            // Set to Focal Target Row
            List<BattleEntity> rowTargets = battleActionData.GetTargets().Where(target => target.column == battleActionData.GetFocalTarget().column).ToList();
            battleActionData.SetTargets(rowTargets);
        }
    }
}
