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

        public override void StartEffect(CombatParticipant sender, IEnumerable<CombatParticipant> recipients, Action<EffectStrategy> finished)
        {
            if (effectStrategies == null) { return; }

            StartCoroutine(sender, DelayedEffect(sender, recipients, finished));
        }

        private IEnumerator DelayedEffect(CombatParticipant sender, IEnumerable<CombatParticipant> recipients, Action<EffectStrategy> finished)
        {
            yield return new WaitForSeconds(delay);
            foreach (EffectStrategy effectStrategy in effectStrategies)
            {
                effectStrategy.StartEffect(sender, recipients, finished);
            }
        }
    }
}