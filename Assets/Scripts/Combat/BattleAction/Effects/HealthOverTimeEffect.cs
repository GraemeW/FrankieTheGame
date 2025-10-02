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
        [SerializeField][Range(0, 1)] float fractionProbabilityToApply = 0.5f;
        [SerializeField] float duration = 10f;
        [SerializeField] float tickPeriod = 3f;
        [SerializeField] float healthChangePerTick = -10f;
        [SerializeField] bool persistAfterCombat = false;

        public override void StartEffect(CombatParticipant sender, IEnumerable<BattleEntity> recipients, DamageType damageType, Action<EffectStrategy> finished)
        {
            if (recipients == null) { return; }
            if (Mathf.Approximately(healthChangePerTick, 0f)) { return; }

            float numberOfTicks = duration / tickPeriod;
            float sign = Mathf.Sign(healthChangePerTick);
            foreach (BattleEntity recipient in recipients)
            {
                float chanceRoll = UnityEngine.Random.Range(0f, 1f);
                if (fractionProbabilityToApply < chanceRoll) { return; }

                PersistentRecurringStatus activeStatusEffect = recipient.combatParticipant.gameObject.AddComponent(typeof(PersistentRecurringStatus)) as PersistentRecurringStatus;

                float modifiedHealthChangePerTick = healthChangePerTick;
                modifiedHealthChangePerTick += damageType switch
                {
                    DamageType.None => 0f,
                    DamageType.Physical => GetPhysicalModifier(sign, sender, recipient.combatParticipant) / numberOfTicks,
                    DamageType.Magical => GetMagicalModifier(sign, sender, recipient.combatParticipant) / numberOfTicks,
                    _ => 0f,
                };

                if (sign > 0)
                {
                    // Quiet for heals -- distracting to hear constant healing
                    activeStatusEffect.Setup(duration, tickPeriod, () => recipient.combatParticipant.AdjustHPQuietly(modifiedHealthChangePerTick), Stat.HP, true, persistAfterCombat);
                }
                else
                {
                    // Loud for damage -- need the feedback on health being taken (both on character and enemy)
                    activeStatusEffect.Setup(duration, tickPeriod, () => recipient.combatParticipant.AdjustHP(modifiedHealthChangePerTick), Stat.HP, false, persistAfterCombat);
                }

                recipient.combatParticipant.AnnounceStateUpdate(StateAlteredType.StatusEffectApplied, activeStatusEffect);
            }

            finished?.Invoke(this);
        }
    }
}
