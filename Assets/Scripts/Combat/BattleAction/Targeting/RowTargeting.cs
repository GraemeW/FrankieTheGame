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
        
        public override void GetTargets(bool? traverseForward, BattleActionData battleActionData, IEnumerable<BattleEntity> activeCharacters,
            IEnumerable<BattleEntity> activeEnemies)
        {
            // Find current target row
            BattleRow currentBattleRow = (battleActionData.targetCount > 0) ? battleActionData.GetFirst().row : _defaultBattleRow;
            
            // Filter
            battleActionData.SetTargets(this.GetCombatParticipantsByType(combatParticipantType, activeCharacters, activeEnemies));
            FilterTargets(battleActionData, filterStrategies);
            if (battleActionData.targetCount == 0) { return; }

            // Pass back as-defined selection on null
            if (traverseForward == null)
            {
                var passTargets = battleActionData.GetTargets().Where(battleEntity => battleEntity.row == currentBattleRow).ToList();
                battleActionData.SetTargets(passTargets);
                return;
            }
            
            // Otherwise traverse
            List<BattleEntity> shiftedTargets = GetShiftedTargets(battleActionData, currentBattleRow, traverseForward.Value);
            battleActionData.SetTargets(shiftedTargets);
        }

        private List<BattleEntity> GetShiftedTargets(BattleActionData battleActionData, BattleRow currentBattleRow, bool traverseForward)
        {
            var battleRows = Enum.GetValues(typeof(BattleRow)).Cast<BattleRow>().ToList();
            battleRows.Remove(BattleRow.Any);
            int currentIndex = battleRows.IndexOf(currentBattleRow);
            
            if (traverseForward)
            {
                currentIndex = (currentIndex == battleRows.Count - 1) ? 0 : currentIndex + 1;
            }
            else
            {
                currentIndex = (currentIndex == 0) ? battleRows.Count - 1 : currentIndex - 1;
            }
            currentBattleRow = battleRows[currentIndex];
            var shiftedTargets = battleActionData.GetTargets().Where(battleEntity => battleEntity.row == currentBattleRow).ToList();
            return shiftedTargets.Count > 0 ? shiftedTargets : GetShiftedTargets(battleActionData, currentBattleRow, traverseForward);
        }

        protected override List<BattleEntity> GetBattleEntitiesByTypeTemplate(CombatParticipantType combatParticipantType, IEnumerable<BattleEntity> activeCharacters,
            IEnumerable<BattleEntity> activeEnemies)
        {
            // Not evaluated -> TargetingStrategyExtension
            return new List<BattleEntity>();
        }
    }
}
