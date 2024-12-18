using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frankie.Stats;

namespace Frankie.Combat
{
    [CreateAssetMenu(fileName = "New Health Effect", menuName = "BattleAction/Effects/Health Effect")]
    public class HealthEffect : EffectStrategy
    {
        [Tooltip("Effective minimum change")][SerializeField] float healthChange = 0f;
        [Tooltip("Added on top as range (0 to jitter), sign based on health change")][SerializeField][Min(0f)] float jitter = 0f;
        [SerializeField] bool applyDamageTypeModifiers = true;
        [SerializeField] bool canMiss = true;
        [SerializeField] bool canCrit = true;
        [SerializeField][Min(1f)] float critMultiplier = 2f;

        public override void StartEffect(CombatParticipant sender, IEnumerable<BattleEntity> recipients, DamageType damageType, Action<EffectStrategy> finished)
        {
            if (recipients == null) { return; }

            float sign = Mathf.Sign(healthChange);
            foreach (BattleEntity recipient in recipients)
            {
                if (!DoesAttackHit(canMiss, sender, recipient.combatParticipant)) { continue; }

                float modifiedHealthChange = healthChange + sign * UnityEngine.Random.Range(0f, jitter);
                if (applyDamageTypeModifiers)
                {
                    modifiedHealthChange += damageType switch
                    {
                        DamageType.None => 0f,
                        DamageType.Physical => GetPhysicalModifier(sign, sender, recipient.combatParticipant),
                        DamageType.Magical => GetMagicalModifier(sign, sender, recipient.combatParticipant),
                        _ => 0f,
                    };
                }

                modifiedHealthChange *= GetCritModifier(canCrit, critMultiplier, sender, recipient.combatParticipant);

                recipient.combatParticipant.AdjustHP(modifiedHealthChange);
            }

            finished?.Invoke(this);
        }
    }
}
