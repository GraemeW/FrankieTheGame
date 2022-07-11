using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Combat
{
    public abstract class TargetPriority : ScriptableObject
    {
        public abstract bool SetTarget(BattleAI battleAI, BattleActionData battleActionData, Skill skill);
    }
}
