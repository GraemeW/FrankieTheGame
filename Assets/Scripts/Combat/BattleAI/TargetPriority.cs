using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Combat
{
    public abstract class TargetPriority : ScriptableObject
    {
        public abstract bool SetTarget(BattleActionData battleActionData, Skill skill, bool isFriendly, List<CombatParticipant> characters, List<CombatParticipant> enemies);
    }
}
