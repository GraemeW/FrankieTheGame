using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Combat
{
    public abstract class TargetingStrategy : ScriptableObject
    {
        [SerializeField] protected CombatParticipantType combatParticipantType = default;
        [SerializeField] protected FilterStrategy[] filterStrategies = null;

        public abstract IEnumerable<CombatParticipant> GetTargets(bool? traverseForward, IEnumerable<CombatParticipant> currentTargets, IEnumerable<CombatParticipant> activeCharacters, IEnumerable<CombatParticipant> activeEnemies);
        protected abstract List<CombatParticipant> GetCombatParticipantsByTypeTemplate(CombatParticipantType combatParticipantType, IEnumerable<CombatParticipant> activeCharacters, IEnumerable<CombatParticipant> activeEnemies);

        protected IEnumerable<CombatParticipant> FilterTargets(IEnumerable<CombatParticipant> potentialTargets, FilterStrategy[] filterStrategies)
        {
            if (filterStrategies != null)
            {
                foreach (FilterStrategy filterStrategy in filterStrategies)
                {
                    potentialTargets = filterStrategy.Filter(potentialTargets);
                }
            }

            return potentialTargets;
        }
    }
}