using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Combat
{
    [CreateAssetMenu(fileName = "New Health Sequence Effect", menuName = "BattleAction/Effects/Health Sequence Effect")]
    public class HealthSequenceEffect : EffectStrategy
    {
        [Tooltip("Effective minimum change")][SerializeField] private float[] healthChangeSequence;
        [Tooltip("Added on top as range (0 to jitter), sign based on health change")][SerializeField][Min(0f)] private float jitter;
        [SerializeField] private float delaySecondsBetweenHits = 0.5f;
        [SerializeField] private bool applyDamageTypeModifiers = true;
        [SerializeField] private bool canMiss = true;
        [SerializeField] private bool canCrit = true;
        [SerializeField][Min(1f)] private float critMultiplier = 2f;

        public override IEnumerator StartEffect(CombatParticipant sender, IList<BattleEntity> recipients, DamageType damageType)
        {
            if (recipients == null) { yield break; }
            if (healthChangeSequence == null || healthChangeSequence.Length == 0) { yield break; }
            
            foreach (float healthChange in healthChangeSequence)
            {
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
                
                yield return new WaitForSeconds(delaySecondsBetweenHits);
            }
        }
    }
}