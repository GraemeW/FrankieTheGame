using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frankie.Stats;

namespace Frankie.Combat
{
    [CreateAssetMenu(fileName = "New DoT Effect", menuName = "BattleAction/Effects/DoT Effect")]
    public class HealthOverTimeEffect : EffectStrategy, IPersistentStatusApplier
    {
        // Tunables
        [SerializeField][Tooltip("Used if statContest is false")][Range(0, 1)] private float fractionProbabilityToApply = 0.5f;
        [SerializeField][Tooltip("Alternate probability derivation via stat contest")] private bool useStatContestProbabilityToApply = false;
        [SerializeField][Tooltip("Used if statContest is true")] private Stat statForContest = Stat.Luck;
        [SerializeField] private float duration = 10f;
        [SerializeField] private float tickPeriod = 3f;
        [SerializeField] private float healthChangePerTick = -10f;
        [SerializeField] private bool persistAfterCombat = false;
        [SerializeField] private int numberOfDuplicateEffectsAllowed = 1;
        
        public override IEnumerator StartEffect(CombatParticipant sender, IList<BattleEntity> recipients, DamageType damageType)
        {
            if (recipients == null) { yield break; }
            if (Mathf.Approximately(healthChangePerTick, 0f)) { yield break; }
            
            foreach (BattleEntity recipient in recipients)
            {
                if (CheckForEffect(recipient.combatParticipant)) { continue; }

                bool probabilityCheck = useStatContestProbabilityToApply
                    ? IPersistentStatusApplier.CheckProbabilityToApply(sender, recipient.combatParticipant, statForContest)
                    : IPersistentStatusApplier.CheckProbabilityToApply(fractionProbabilityToApply);
                if (!probabilityCheck) { continue; }
                
                PersistentStatus activeStatusEffect = Apply(sender, recipient.combatParticipant, damageType);
                if (activeStatusEffect== null) { continue; }
                
                recipient.combatParticipant.AnnounceStateUpdate(StateAlteredType.StatusEffectApplied, activeStatusEffect);
            }
        }

        public bool CheckForEffect(CombatParticipant recipient)
        {
            return PersistentStatus.DoesEffectExist(recipient, effectGUID, numberOfDuplicateEffectsAllowed, duration);
        }

        public PersistentStatus Apply(CombatParticipant sender, CombatParticipant recipient, DamageType damageType)
        {
            float numberOfTicks = duration / tickPeriod;
            float sign = Mathf.Sign(healthChangePerTick);
            
            PersistentRecurringStatus activeStatusEffect = recipient.gameObject.AddComponent(typeof(PersistentRecurringStatus)) as PersistentRecurringStatus;
            if (activeStatusEffect == null) { return null; }
                
            float modifiedHealthChangePerTick = healthChangePerTick;
            modifiedHealthChangePerTick += damageType switch
            {
                DamageType.None => 0f,
                DamageType.Physical => GetPhysicalModifier(sign, sender, recipient) / numberOfTicks,
                DamageType.Magical => GetMagicalModifier(sign, sender, recipient) / numberOfTicks,
                _ => 0f,
            };

            if (sign > 0)
            {
                // Quiet for heals -- distracting to hear constant healing
                activeStatusEffect.Setup(effectGUID, duration, tickPeriod, () => recipient.AdjustHPQuietly(modifiedHealthChangePerTick), Stat.HP, true, persistAfterCombat);
            }
            else
            {
                // Loud for damage -- need the feedback on health being taken (both on character and enemy)
                activeStatusEffect.Setup(effectGUID, duration, tickPeriod, () => recipient.AdjustHP(modifiedHealthChangePerTick), Stat.HP, false, persistAfterCombat);
            }
            return activeStatusEffect;
        }
    }
}
