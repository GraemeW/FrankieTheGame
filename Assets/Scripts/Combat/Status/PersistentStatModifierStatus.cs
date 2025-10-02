using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frankie.Stats;

namespace Frankie.Combat
{
    public class PersistentStatModifierStatus : PersistentStatus, IModifierProvider
    {
        // Tunables
        float value = 0f;

        public void Setup(float duration, Stat stat, float value, bool persistAfterBattle = false)
        {
            if (Mathf.Approximately(value, 0f)) { Destroy(this); }

            base.Setup(duration, persistAfterBattle);

            this.statusEffectType = stat;
            this.value = value;
            this.isIncrease = (this.value > 0f);
        }

        public IEnumerable<float> GetAdditiveModifiers(Stat stat)
        {
            if (!active) { yield break; }

            if (this.statusEffectType == stat)
            {
                yield return value;
            }
        }
    }

}
