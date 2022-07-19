using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frankie.Stats;

namespace Frankie.Combat
{
    [CreateAssetMenu(fileName = "New DoT Effect", menuName = "BattleAction/Effects/DoT Effect")]
    public class HealthOverTimeEffect : EffectStrategy
    {
        [SerializeField] [Range(0, 1)] float fractionProbabilityToApply = 0.5f;
        [SerializeField] StatusType statusEffectType = default;
        [SerializeField] float duration = 10f;
        [SerializeField] float tickPeriod = 3f;
        [SerializeField] float healthChangePerTick = -10f;
        [SerializeField] bool persistAfterCombat = false;

        public override void StartEffect(CombatParticipant sender, IEnumerable<CombatParticipant> recipients, DamageType damageType, Action<EffectStrategy> finished)
        {
            if (recipients == null) { return; }

            float numberOfTicks = duration / tickPeriod;
            float sign = Mathf.Sign(healthChangePerTick);
            foreach (CombatParticipant recipient in recipients)
            {
                float chanceRoll = UnityEngine.Random.Range(0f, 1f);
                if (fractionProbabilityToApply < chanceRoll) { return; }

                PersistentRecurringStatus activeStatusEffect = recipient.gameObject.AddComponent(typeof(PersistentRecurringStatus)) as PersistentRecurringStatus;

                float modifiedHealthChangePerTick = healthChangePerTick;
                modifiedHealthChangePerTick += damageType switch
                {
                    DamageType.None => 0f,
                    DamageType.Physical => GetPhysicalModifier(sign, sender, recipient) / numberOfTicks,
                    DamageType.Magical => GetMagicalModifier(sign, sender, recipient) / numberOfTicks,
                    _ => 0f,
                };

                activeStatusEffect.Setup(statusEffectType, duration, tickPeriod, () => recipient.AdjustHPQuietly(modifiedHealthChangePerTick), persistAfterCombat);

                recipient.AnnounceStateUpdate(new StateAlteredData(StateAlteredType.StatusEffectApplied, statusEffectType));
            }

            finished?.Invoke(this);
        }
    }
}