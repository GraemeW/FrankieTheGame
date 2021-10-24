using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Combat
{
    public abstract class TargetingStrategy : ScriptableObject
    {
        public abstract IEnumerable<CombatParticipant> GetTargets(bool? traverseForward, IEnumerable<CombatParticipant> currentTargets, IEnumerable<CombatParticipant> activeCharacters, IEnumerable<CombatParticipant> activeEnemies);
        protected abstract IEnumerable<CombatParticipant> GetCombatParticipantsByTypeTemplate(CombatParticipantType combatParticipantType, IEnumerable<CombatParticipant> activeCharacters, IEnumerable<CombatParticipant> activeEnemies);
    }
}