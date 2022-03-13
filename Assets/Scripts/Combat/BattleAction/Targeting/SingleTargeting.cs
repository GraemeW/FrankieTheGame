using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Frankie.Combat
{
    [CreateAssetMenu(fileName = "New Single Targeting", menuName = "BattleAction/Targeting/Single Target")]
    public class SingleTargeting : TargetingStrategy
    {
        public override void GetTargets(bool? traverseForward, BattleActionData battleActionData, 
            IEnumerable<CombatParticipant> activeCharacters, IEnumerable<CombatParticipant> activeEnemies)
        {
            // Collapse target list to single target -- pull first if available
            CombatParticipant newTarget = null;
            if (battleActionData.targetCount > 0)
            {
                newTarget = battleActionData.GetFirst();
            }

            // Separate out overall set & filter
            battleActionData.SetTargets(this.GetCombatParticipantsByType(combatParticipantType, activeCharacters, activeEnemies));
            FilterTargets(battleActionData, filterStrategies);
            if (battleActionData.targetCount == 0) { return; }

            // Special handling for null traverse:  No traversing -- pass the target back after filtering
            if (traverseForward == null)
            {
                if (battleActionData.GetTargets().Contains(newTarget))
                {
                    battleActionData.SetTargets(newTarget);
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

            foreach (CombatParticipant combatParticipant in battleActionData.GetTargets())
            {
                if (returnOnNextIteration) // B) select target, break
                {
                    newTarget = combatParticipant;
                    break;
                }

                returnOnNextIteration = (combatParticipant == newTarget); // A) Match to current index -- return on next target
            }
            if (newTarget != null) { battleActionData.SetTargets(newTarget); return; } // C) set target, return


            // Matched on last index -- return top of the list
            if (returnOnNextIteration)
            {
                if (traverseForward == true) { newTarget = battleActionData.GetFirst(); }
                else if (traverseForward == false) { newTarget = battleActionData.GetLast(); }
                battleActionData.SetTargets(newTarget);
            }

            // Special case -- never matched to current target, send first available up the chain
            battleActionData.SetTargets(battleActionData.GetFirst());
        }

        protected override List<CombatParticipant> GetCombatParticipantsByTypeTemplate(CombatParticipantType combatParticipantType, 
            IEnumerable<CombatParticipant> activeCharacters, IEnumerable<CombatParticipant> activeEnemies)
        {
            // Not evaluated -> TargetingStrategyExtension
            return new List<CombatParticipant>();
        }
    }
}