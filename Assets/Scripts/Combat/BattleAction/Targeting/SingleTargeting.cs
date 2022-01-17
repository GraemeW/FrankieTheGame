using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Frankie.Combat
{
    [CreateAssetMenu(fileName = "New Single Targeting", menuName = "BattleAction/Targeting/Single Target")]
    public class SingleTargeting : TargetingStrategy
    {
        public override IEnumerable<CombatParticipant> GetTargets(bool? traverseForward, IEnumerable<CombatParticipant> currentTargets, IEnumerable<CombatParticipant> activeCharacters, IEnumerable<CombatParticipant> activeEnemies)
        {
            // Collapse target list to single target -- pull first if available
            CombatParticipant[] passedOriginalTargets = currentTargets?.ToArray();
            CombatParticipant newTarget = null;
            if (passedOriginalTargets != null && passedOriginalTargets.Length > 0)
            {
                newTarget = passedOriginalTargets.First();
            }

            // Special handling
            // No traversing -- pass the target back
            if (traverseForward == null && newTarget != null)
            {
                yield return newTarget;
                yield break;
            }

            // Separate out overall set & filter
            // Note:  No need to declare local copies of characters &  enemies enumerables -- placed in new list via GetCombatParticipantByType
            List<CombatParticipant> potentialTargets = this.GetCombatParticipantsByType(combatParticipantType, 
                FilterTargets(activeCharacters, filterStrategies), 
                FilterTargets(activeEnemies, filterStrategies));
            if (potentialTargets.Count == 0) { yield break; }

            // Special handling
            // No current target -- pass first target back
            if (passedOriginalTargets == null || passedOriginalTargets.Length == 0)
            {
                CombatParticipant defaultTarget = potentialTargets.FirstOrDefault();
                if (defaultTarget != null) { yield return defaultTarget; }
                yield break;
            }

            // Finally iterate through to find next targets
            bool returnOnNextIteration = false;
            if (traverseForward == false)
            {
                potentialTargets.Reverse();
            }
            foreach (CombatParticipant combatParticipant in potentialTargets)
            {
                if (returnOnNextIteration)
                {
                    yield return combatParticipant;
                    yield break;
                }

                returnOnNextIteration = (combatParticipant == newTarget); // Match to current index -- return on next target
            }

            // Matched on last index -- return top of the list
            if (returnOnNextIteration)
            {
                if (traverseForward == true) { newTarget = potentialTargets.First(); }
                else if (traverseForward == false) { newTarget = potentialTargets.Last(); }
                yield return newTarget;
                yield break;
            }

            // Special case -- never matched to current target, send first available up the chain
            yield return potentialTargets.First(); yield break;
        }

        protected override List<CombatParticipant> GetCombatParticipantsByTypeTemplate(CombatParticipantType combatParticipantType, IEnumerable<CombatParticipant> activeCharacters, IEnumerable<CombatParticipant> activeEnemies)
        {
            // Not evaluated -> TargetingStrategyExtension
            return new List<CombatParticipant>();
        }
    }
}