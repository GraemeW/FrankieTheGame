using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Combat
{
    public abstract class TargetingStrategy : ScriptableObject
    {
        [SerializeField] protected CombatParticipantType combatParticipantType = default;
        [SerializeField] protected FilterStrategy[] filterStrategies = null;

        public abstract void GetTargets(bool? traverseForward, BattleActionData battleActionData,
            IEnumerable<CombatParticipant> activeCharacters, IEnumerable<CombatParticipant> activeEnemies);
        protected abstract List<CombatParticipant> GetCombatParticipantsByTypeTemplate(CombatParticipantType combatParticipantType, IEnumerable<CombatParticipant> activeCharacters, IEnumerable<CombatParticipant> activeEnemies);

        protected void FilterTargets(BattleActionData battleActionData, FilterStrategy[] filterStrategies)
        {
            if (filterStrategies != null)
            {
                foreach (FilterStrategy filterStrategy in filterStrategies)
                {
                    battleActionData.SetTargets(filterStrategy.Filter(battleActionData.GetTargets()));
                }
            }
        }
    }
}