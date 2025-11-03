using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Combat
{
    [CreateAssetMenu(fileName = "New Delay Composite Effect", menuName = "BattleAction/Effects/Delay Composite Effect")]
    public class DelayCompositeEffect : EffectStrategy
    {
        [SerializeField] private float delay = 0.5f;
        [SerializeField] private EffectStrategy[] effectStrategies;

        public override IEnumerator StartEffect(CombatParticipant sender, IList<BattleEntity> recipients, DamageType damageType)
        {
            if (effectStrategies == null) { yield break; }
            
            if (BattleEventBus.inBattle) { yield return new WaitForSeconds(delay); }
            foreach (EffectStrategy effectStrategy in effectStrategies)
            {
                if (effectStrategy == null) { continue; }

                yield return effectStrategy.StartEffect(sender, recipients, damageType);
            }
        }
    }
}
