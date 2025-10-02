using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Combat
{
    [CreateAssetMenu(fileName = "New Single Targeting", menuName = "BattleAction/Targeting/Single Target")]
    public class SingleTargeting : TargetingStrategy
    {
        public override void GetTargets(bool? traverseForward, BattleActionData battleActionData,
            IEnumerable<BattleEntity> activeCharacters, IEnumerable<BattleEntity> activeEnemies)
        {
            // Collapse target list to single target -- pull first if available
            BattleEntity passTarget = null;
            if (battleActionData.targetCount > 0)
            {
                passTarget = battleActionData.GetFirst();
            }

            // Separate out overall set & filter
            battleActionData.SetTargets(this.GetCombatParticipantsByType(combatParticipantType, activeCharacters, activeEnemies));
            FilterTargets(battleActionData, filterStrategies);
            if (battleActionData.targetCount == 0) { return; }

            // Special handling for null traverse:  No traversing -- pass the target back after filtering
            if (traverseForward == null)
            {
                if (battleActionData.GetTargets().Contains(passTarget))
                {
                    battleActionData.SetTargets(passTarget);
                }
                else
                {
                    battleActionData.ClearTargets();
                }
                return;
            }

            // Finally iterate through to find next targets
            bool returnOnNextIteration = false;
            if (traverseForward == false)
            {
                battleActionData.ReverseTargets();
            }

            foreach (BattleEntity battleEntity in battleActionData.GetTargets())
            {
                if (returnOnNextIteration) // B) select target, break
                {
                    passTarget = battleEntity;
                    break;
                }

                returnOnNextIteration = (battleEntity == passTarget); // A) Match to current index -- return on next target
            }
            if (passTarget != null) { battleActionData.SetTargets(passTarget); return; } // C) set target, return


            // Matched on last index -- return top of the list
            if (returnOnNextIteration)
            {
                if (traverseForward == true) { passTarget = battleActionData.GetFirst(); }
                else if (traverseForward == false) { passTarget = battleActionData.GetLast(); }
                battleActionData.SetTargets(passTarget);
            }

            // Special case -- never matched to current target, send first available up the chain
            battleActionData.SetTargets(battleActionData.GetFirst());
        }

        protected override List<BattleEntity> GetBattleEntitiesByTypeTemplate(CombatParticipantType combatParticipantType,
            IEnumerable<BattleEntity> activeCharacters, IEnumerable<BattleEntity> activeEnemies)
        {
            // Not evaluated -> TargetingStrategyExtension
            return new List<BattleEntity>();
        }
    }
}
