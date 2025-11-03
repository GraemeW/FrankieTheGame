using System.Collections.Generic;
using UnityEngine;
using Frankie.Stats;

namespace Frankie.Combat
{
    public class PersistentStatModifierStatus : PersistentStatus, IModifierProvider
    {
        // Tunables
        private float value;

        public void Setup(float duration, Stat stat, float setValue, bool persistAfterBattle = false)
        {
            if (Mathf.Approximately(setValue, 0f)) { Destroy(this); }

            base.Setup(duration, persistAfterBattle);

            statusEffectType = stat;
            value = setValue;
            isIncrease = (value > 0f);
        }

        public IEnumerable<float> GetAdditiveModifiers(Stat stat)
        {
            if (!active) { yield break; }

            if (statusEffectType == stat)
            {
                yield return value;
            }
        }
    }
}
