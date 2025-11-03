using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Frankie.Combat
{
    [CreateAssetMenu(fileName = "New Row Targeting", menuName = "BattleAction/Targeting/Row Target")]
    public class RowTargeting : TargetingStrategy
    {
        private const BattleRow _defaultBattleRow = BattleRow.Middle;
        
        public override void SetTargets(TargetingNavigationType targetingNavigationType, BattleActionData battleActionData, IEnumerable<BattleEntity> activeCharacters,
            IEnumerable<BattleEntity> activeEnemies)
        {
            // Filter
            battleActionData.SetTargets(GetCombatParticipantsByType(combatParticipantType, activeCharacters, activeEnemies));
            FilterTargets(battleActionData, filterStrategies);
            if (!battleActionData.HasTargets()) { return; }

            // Set Focal Target
            battleActionData.SetFocalTarget(targetingNavigationType);
            
            // Set to Focal Target Row
            List<BattleEntity> rowTargets = battleActionData.GetTargets().Where(target => target.row == battleActionData.GetFocalTarget().row).ToList();
            battleActionData.SetTargets(rowTargets);
        }
    }
}
