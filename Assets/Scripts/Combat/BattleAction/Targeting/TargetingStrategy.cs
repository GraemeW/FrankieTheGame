using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Combat
{
    public abstract class TargetingStrategy : ScriptableObject
    {
        [SerializeField] protected CombatParticipantType combatParticipantType;
        [SerializeField] protected FilterStrategy[] filterStrategies;

        public abstract void GetTargets(bool? traverseForward, BattleActionData battleActionData,
            IEnumerable<BattleEntity> activeCharacters, IEnumerable<BattleEntity> activeEnemies);
        protected abstract List<BattleEntity> GetBattleEntitiesByTypeTemplate(CombatParticipantType combatParticipantType, IEnumerable<BattleEntity> activeCharacters, IEnumerable<BattleEntity> activeEnemies);

        protected static void FilterTargets(BattleActionData battleActionData, FilterStrategy[] tryFilterStrategies)
        {
            if (tryFilterStrategies == null) return;
            foreach (FilterStrategy filterStrategy in tryFilterStrategies)
            {
                battleActionData.SetTargets(filterStrategy.Filter(battleActionData.GetTargets()));
            }
        }
    }
}
