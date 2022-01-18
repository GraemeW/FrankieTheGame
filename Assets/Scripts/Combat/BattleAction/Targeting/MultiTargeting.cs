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

        public override void GetTargets(bool? traverseForward, BattleActionData battleActionData, 
            IEnumerable<CombatParticipant> activeCharacters, IEnumerable<CombatParticipant> activeEnemies)
        {
            // Collapse target list to expected number to hit
            List<CombatParticipant> newTargets = new List<CombatParticipant>();
            if (battleActionData.targetCount > 0)
            {
                int modifiedPassedLength = 0;
                foreach (CombatParticipant combatParticipant in battleActionData.GetTargets())
                {
                    newTargets.Add(combatParticipant);
                    modifiedPassedLength++;

                    if (!overrideToHitEverything && modifiedPassedLength >= numberOfEnemiesToHit) { break; }
                }
            }

            // Special handling
            // No traversing -- pass the target back
            if (traverseForward == null)
            {
                battleActionData.SetTargets(newTargets);
                return;
            }

            // Separate out overall set & filter
            battleActionData.SetTargets(this.GetCombatParticipantsByType(combatParticipantType, activeCharacters, activeEnemies));
            FilterTargets(battleActionData, filterStrategies);
            if (battleActionData.targetCount == 0) { return; }

            // Special handling for hit everything -- return the whole set & break
            if (overrideToHitEverything || battleActionData.targetCount > numberOfEnemiesToHit)
            {
                return;
            }

            // Finally iterate through to find next targets
            CombatParticipant oldIndexTarget = newTargets?.First();
            if (traverseForward == false)
            {
                battleActionData.ReverseTargets();
                oldIndexTarget = newTargets?.Last();
            }
            List<CombatParticipant> shiftedTargets = GetShiftedTargets(battleActionData, oldIndexTarget).ToList(); // Define locally since iterating over the list in battleActionData
            battleActionData.SetTargets(shiftedTargets);
        }

        private IEnumerable<CombatParticipant> GetShiftedTargets(BattleActionData battleActionData, CombatParticipant oldIndexTarget)
        {
            bool indexFound = false;
            int targetLength = 0;
            List<CombatParticipant> cycledTargets = new List<CombatParticipant>();
            foreach (CombatParticipant combatParticipant in battleActionData.GetTargets())
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