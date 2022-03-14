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
            List<CombatParticipant> passTargets = new List<CombatParticipant>();
            if (battleActionData.targetCount > 0)
            {
                int modifiedPassedLength = 0;
                foreach (CombatParticipant combatParticipant in battleActionData.GetTargets())
                {
                    passTargets.Add(combatParticipant);
                    modifiedPassedLength++;

                    if (!overrideToHitEverything && modifiedPassedLength >= numberOfEnemiesToHit) { break; }
                }
            }

            // Separate out overall set & filter
            battleActionData.SetTargets(this.GetCombatParticipantsByType(combatParticipantType, activeCharacters, activeEnemies));
            FilterTargets(battleActionData, filterStrategies);
            if (battleActionData.targetCount == 0) { return; }
            
            if (overrideToHitEverything) // Return immediately after filtering -- whole set, no need to iterate
            {
                return;
            }

            // Finally iterate through to find next targets
            CombatParticipant oldIndexTarget = passTargets?.First();
            if (traverseForward == false)
            {
                battleActionData.ReverseTargets();
                oldIndexTarget = passTargets?.Last();
            }

            List<CombatParticipant> shiftedTargets; // Define locally since iterating over the list in battleActionData
            if (traverseForward == null)
            {
                // Special handling null travers forward -- pass back set as it was received (modifying up if entities filtered out)
                shiftedTargets = GetShiftedTargets(battleActionData, oldIndexTarget, true).ToList();
            }
            else
            {
                shiftedTargets = GetShiftedTargets(battleActionData, oldIndexTarget).ToList(); 
            }
            battleActionData.SetTargets(shiftedTargets);
        }

        private IEnumerable<CombatParticipant> GetShiftedTargets(BattleActionData battleActionData, CombatParticipant oldIndexTarget, bool doNotShift = false)
        {
            bool indexFound = false;
            int targetLength = 0;
            List<CombatParticipant> cycledTargets = new List<CombatParticipant>();
            foreach (CombatParticipant combatParticipant in battleActionData.GetTargets())
            {
                if (!indexFound)
                {
                    if (combatParticipant == oldIndexTarget)
                    { 
                        indexFound = true;
                        if (doNotShift)
                        {
                            yield return combatParticipant;
                            continue;
                        }
                    }
                    cycledTargets.Add(combatParticipant);
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