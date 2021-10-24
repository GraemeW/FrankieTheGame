using Frankie.Stats;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Combat
{
    public class PersistentStatModifierStatus : PersistentStatus, IModifierProvider
    {
        // Tunables
        Stat stat = default;
        float value = 0f;

        public void Setup(StatusType statusEffectType, float duration, Stat stat, float value, bool persistAfterBattle = false)
        {
            base.Setup(statusEffectType, duration, persistAfterBattle);

            this.stat = stat;
            this.value = value;
        }

        public IEnumerable<float> GetAdditiveModifiers(Stat stat)
        {
            if (this.stat == stat)
            {
                yield return value;
            }
            yield break;
        }
    }

}
