using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frankie.Stats;

namespace Frankie.Combat
{
    [CreateAssetMenu(fileName = "New DoT Effect", menuName = "BattleAction/Effects/DoT Effect")]
    public class HealthOverTimeEffect : EffectStrategy
    {
        // Tunables
        [SerializeField][Range(0, 1)] private float fractionProbabilityToApply = 0.5f;
        [SerializeField] private float duration = 10f;
        [SerializeField] private float tickPeriod = 3f;
        [SerializeField] private float healthChangePerTick = -10f;
        [SerializeField] private bool persistAfterCombat = false;
        [SerializeField] private int numberOfDuplicateEffectsAllowed = 1;
        
        public override IEnumerator StartEffect(CombatParticipant sender, IList<BattleEntity> recipients, DamageType damageType)
        {
            if (recipients == null) { yield break; }
            if (Mathf.Approximately(healthChangePerTick, 0f)) { yield break; }

            float numberOfTicks = duration / tickPeriod;
            float sign = Mathf.Sign(healthChangePerTick);
            foreach (BattleEntity recipient in recipients)
            {
                float chanceRoll = UnityEngine.Random.Range(0f, 1f);
                if (fractionProbabilityToApply < chanceRoll) { continue; }

                if (PersistentStatus.DoesEffectExist(recipient, effectGUID, numberOfDuplicateEffectsAllowed, duration)) continue;

                PersistentRecurringStatus activeStatusEffect = recipient.combatParticipant.gameObject.AddComponent(typeof(PersistentRecurringStatus)) as PersistentRecurringStatus;
                if (activeStatusEffect == null) { continue; }
                
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
                    activeStatusEffect.Setup(effectGUID, duration, tickPeriod, () => recipient.combatParticipant.AdjustHPQuietly(modifiedHealthChangePerTick), Stat.HP, true, persistAfterCombat);
                }
                else
                {
                    // Loud for damage -- need the feedback on health being taken (both on character and enemy)
                    activeStatusEffect.Setup(effectGUID, duration, tickPeriod, () => recipient.combatParticipant.AdjustHP(modifiedHealthChangePerTick), Stat.HP, false, persistAfterCombat);
                }

                recipient.combatParticipant.AnnounceStateUpdate(StateAlteredType.StatusEffectApplied, activeStatusEffect);
            }
        }
    }
}
