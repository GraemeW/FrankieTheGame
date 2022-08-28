using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Combat
{
    [CreateAssetMenu(fileName = "New Delay Composite Effect", menuName = "BattleAction/Effects/Delay Composite Effect")]
    public class DelayCompositeEffect : EffectStrategy
    {
        [SerializeField] float delay = 0f;
        [SerializeField] EffectStrategy[] effectStrategies = null;

        public override void StartEffect(CombatParticipant sender, IEnumerable<BattleEntity> recipients, DamageType damageType, Action<EffectStrategy> finished)
        {
            if (effectStrategies == null) { return; }

            StartCoroutine(sender, DelayedEffect(sender, recipients, damageType, finished));
        }

        private IEnumerator DelayedEffect(CombatParticipant sender, IEnumerable<BattleEntity> recipients, DamageType damageType, Action<EffectStrategy> finished)
        {
            yield return new WaitForSeconds(delay);
            foreach (EffectStrategy effectStrategy in effectStrategies)
            {
                if (effectStrategy == null) { continue; }

                effectStrategy.StartEffect(sender, recipients, damageType, finished);
            }
        }

        public EffectStrategy[] GetEffectStrategies()
        {
            return effectStrategies;
        }
    }
}
