using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Frankie.Combat
{
    [CreateAssetMenu(fileName = "New Multi Targeting", menuName = "BattleAction/Targeting/Multi Target")]
    public class MultiTargeting : TargetingStrategy
    {
        [SerializeField] bool overrideToHitEverything = false;
        [SerializeField] [Min(0)] int numberOfEnemiesToHit = 2;

        public override IEnumerable<CombatParticipant> GetTargets(bool? traverseForward, IEnumerable<CombatParticipant> currentTargets, IEnumerable<CombatParticipant> activeCharacters, IEnumerable<CombatParticipant> activeEnemies)
        {
            CombatParticipant[] passedOriginalTargets = currentTargets?.ToArray();

            // Collapse target list to expected number to hit
            List<CombatParticipant> newTargets = new List<CombatParticipant>();
            if (passedOriginalTargets != null && passedOriginalTargets.Length > 0)
            {
                int modifiedPassedLength = 0;
                foreach (CombatParticipant combatParticipant in passedOriginalTargets)
                {
                    newTargets.Add(combatParticipant);
                    modifiedPassedLength++;

                    if (modifiedPassedLength >= numberOfEnemiesToHit) { break; }
                }
            }

            // Special handling
            // No traversing -- pass the target back
            if (traverseForward == null)
            {
                foreach (CombatParticipant combatParticipant in newTargets)
                {
                    yield return combatParticipant;
                }
                yield break;
            }

            // Separate out overall set & filter
            // Note:  No need to declare local copies of characters &  enemies enumerables -- placed in new list via GetCombatParticipantByType
            List<CombatParticipant> potentialTargets = this.GetCombatParticipantsByType(combatParticipantType,
                FilterTargets(activeCharacters, filterStrategies),
                FilterTargets(activeEnemies, filterStrategies));
            if (potentialTargets.Count == 0) { yield break; }

            // Special handling for hit everything -- return the whole set & break
            if (overrideToHitEverything || potentialTargets.Count() > numberOfEnemiesToHit)
            {
                foreach (CombatParticipant combatParticipant in potentialTargets)
                {
                    yield return combatParticipant;
                }
                yield break;
            }

            // Finally iterate through to find next targets
            CombatParticipant oldIndexTarget = newTargets?.First();
            bool indexFound = false;
            if (traverseForward == false)
            {
                potentialTargets.Reverse();
            }

            int targetLength = 0;
            List<CombatParticipant> cycledTargets = new List<CombatParticipant>();
            foreach (CombatParticipant combatParticipant in potentialTargets)
            {
                if (!indexFound)
                {
                    cycledTargets.Add(combatParticipant);
                    if (combatParticipant == oldIndexTarget) { indexFound = true; }
                }
                else
                {
                    yield return combatParticipant;
                    targetLength++;
                }

                if (targetLength >= numberOfEnemiesToHit) { yield break; }
            }

            foreach (CombatParticipant combatParticipant in cycledTargets)
            {
                yield return combatParticipant;
                targetLength++;

                if (targetLength >= numberOfEnemiesToHit) { yield break; }
            }

        }

        protected override List<CombatParticipant> GetCombatParticipantsByTypeTemplate(CombatParticipantType combatParticipantType, IEnumerable<CombatParticipant> activeCharacters, IEnumerable<CombatParticipant> activeEnemies)
        {
            // Not evaluated -> TargetingStrategyExtension
            return new List<CombatParticipant>();
        }
    }
}