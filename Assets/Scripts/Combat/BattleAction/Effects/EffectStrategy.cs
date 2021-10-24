using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Combat
{
    public abstract class EffectStrategy : ScriptableObject
    {
        public abstract void StartEffect(CombatParticipant sender, IEnumerable<CombatParticipant> recipients);
    }
}