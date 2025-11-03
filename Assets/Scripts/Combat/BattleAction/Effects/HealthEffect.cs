using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Combat
{
    [CreateAssetMenu(fileName = "New Health Effect", menuName = "BattleAction/Effects/Health Effect")]
    public class HealthEffect : EffectStrategy
    {
        [Tooltip("Effective minimum change")][SerializeField] private float healthChange;
        [Tooltip("Added on top as range (0 to jitter), sign based on health change")][SerializeField][Min(0f)] private float jitter;
        [SerializeField] private bool applyDamageTypeModifiers = true;
        [SerializeField] private bool canMiss = true;
        [SerializeField] private bool canCrit = true;
        [SerializeField][Min(1f)] private float critMultiplier = 2f;

        public override IEnumerator StartEffect(CombatParticipant sender, IList<BattleEntity> recipients, DamageType damageType)
        {
            if (recipients == null) { yield break; }

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
        }
    }
}
