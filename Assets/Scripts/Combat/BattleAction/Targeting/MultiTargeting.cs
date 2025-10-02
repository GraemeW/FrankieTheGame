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
        [SerializeField][Min(0)] int numberOfEnemiesToHit = 2;

        public override void GetTargets(bool? traverseForward, BattleActionData battleActionData,
            IEnumerable<BattleEntity> activeCharacters, IEnumerable<BattleEntity> activeEnemies)
        {
            // Collapse target list to expected number to hit
            List<BattleEntity> passTargets = new List<BattleEntity>();
            if (battleActionData.targetCount > 0)
            {
                int modifiedPassedLength = 0;
                foreach (BattleEntity battleEntity in battleActionData.GetTargets())
                {
                    passTargets.Add(battleEntity);
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
            BattleEntity oldIndexTarget = passTargets?.First();
            if (traverseForward == false)
            {
                battleActionData.ReverseTargets();
                oldIndexTarget = passTargets?.Last();
            }

            List<BattleEntity> shiftedTargets; // Define locally since iterating over the list in battleActionData
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

        private IEnumerable<BattleEntity> GetShiftedTargets(BattleActionData battleActionData, BattleEntity oldIndexTarget, bool doNotShift = false)
        {
            bool indexFound = false;
            int targetLength = 0;
            List<BattleEntity> cycledTargets = new List<BattleEntity>();
            foreach (BattleEntity battleEntity in battleActionData.GetTargets())
            {
                if (!indexFound)
                {
                    if (battleEntity == oldIndexTarget)
                    {
                        indexFound = true;
                        if (doNotShift)
                        {
                            yield return battleEntity;
                            continue;
                        }
                    }
                    cycledTargets.Add(battleEntity);
                }
                else
                {
                    yield return battleEntity;
                    targetLength++;
                }

                if (targetLength >= numberOfEnemiesToHit) { yield break; }
            }

            foreach (BattleEntity battleEntity in cycledTargets)
            {
                yield return battleEntity;
                targetLength++;

                if (targetLength >= numberOfEnemiesToHit) { yield break; }
            }
        }

        protected override List<BattleEntity> GetBattleEntitiesByTypeTemplate(CombatParticipantType combatParticipantType, IEnumerable<BattleEntity> activeCharacters, IEnumerable<BattleEntity> activeEnemies)
        {
            // Not evaluated -> TargetingStrategyExtension
            return new List<BattleEntity>();
        }
    }
}
