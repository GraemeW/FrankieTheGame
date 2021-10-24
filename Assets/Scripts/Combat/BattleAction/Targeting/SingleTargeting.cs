using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Frankie.Combat
{
    [CreateAssetMenu(fileName = "New Single Targeting", menuName = "BattleAction/Targeting/Single Target")]
    public class SingleTargeting : TargetingStrategy
    {
        [SerializeField] CombatParticipantType combatParticipantType = default;
        [SerializeField] FilterStrategy[] filterStrategies = null;

        public override IEnumerable<CombatParticipant> GetTargets(bool? traverseForward, IEnumerable<CombatParticipant> currentTargets, IEnumerable<CombatParticipant> activeCharacters, IEnumerable<CombatParticipant> activeEnemies)
        {
            // Special handling for no traverse -- pass back the target
            if (traverseForward == null)
            {
                CombatParticipant defaultCharacter = currentTargets.First();
                if (defaultCharacter != null) { yield return currentTargets.First(); }
                yield break;
            }

            // Separate out overall set
            IEnumerable<CombatParticipant> potentialTargets = this.GetCombatParticipantsByType(combatParticipantType, activeCharacters, activeEnemies);

            // Filter
            if (filterStrategies != null)
            {
                foreach (FilterStrategy filterStrategy in filterStrategies)
                {
                    potentialTargets = filterStrategy.Filter(potentialTargets);
                }
            }
            if (potentialTargets.Count() == 0) { yield break; }

            // Special handling no current target
            if (currentTargets == null || currentTargets.Count() == 0)
            {
                CombatParticipant defaultTarget = potentialTargets.First();
                if (defaultTarget != null) { yield return defaultTarget; }
                yield break;
            }

            // Find next target
            CombatParticipant currentTarget = currentTargets.First(); // Expectation is this would be single target, handling edge case

            bool returnOnNextIteration = false;
            if (traverseForward == true)
            {
                foreach(CombatParticipant combatParticipant in potentialTargets)
                {
                    if (returnOnNextIteration)
                    {
                        yield return combatParticipant;
                        yield break;
                    }

                    if (combatParticipant == currentTarget) { returnOnNextIteration = true; }
                }

                if (returnOnNextIteration) 
                {
                    currentTarget = potentialTargets.First();
                    if (currentTarget != null) { yield return currentTarget; }
                    yield break;
                }
            }
            else if (traverseForward == false)
            {
                foreach(CombatParticipant combatParticipant in potentialTargets.Reverse())
                {
                    if (returnOnNextIteration)
                    {
                        yield return combatParticipant;
                    }

                    if (combatParticipant == currentTarget) { returnOnNextIteration = true; }
                }

                if (returnOnNextIteration)
                {
                    currentTarget = potentialTargets.Last();
                    if (currentTarget != null) { yield return currentTarget; }
                    yield break;
                }
            }

            // Special case -- never matched to current target, send first available up the chain
            yield return potentialTargets.First(); yield break;
        }

        protected override IEnumerable<CombatParticipant> GetCombatParticipantsByTypeTemplate(CombatParticipantType combatParticipantType, IEnumerable<CombatParticipant> activeCharacters, IEnumerable<CombatParticipant> activeEnemies)
        {
            // Not evaluated -> TargetingStrategyExtension
            yield break;
        }
    }
}