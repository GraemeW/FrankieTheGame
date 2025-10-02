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
            IEnumerable<BattleEntity> activeCharacters, IEnumerable<BattleEntity> activeEnemies);
        protected abstract List<BattleEntity> GetBattleEntitiesByTypeTemplate(CombatParticipantType combatParticipantType, IEnumerable<BattleEntity> activeCharacters, IEnumerable<BattleEntity> activeEnemies);

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
