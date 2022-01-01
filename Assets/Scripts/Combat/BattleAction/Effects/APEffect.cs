using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Combat
{
    [CreateAssetMenu(fileName = "New AP Effect", menuName = "BattleAction/Effects/AP Effect")]
    public class APEffect : EffectStrategy
    {
        [SerializeField] float apChange = 0f;
        [Tooltip("Additional variability on base AP change")] [SerializeField] [Min(0f)] float jitter = 0f;

        public override void StartEffect(CombatParticipant sender, IEnumerable<CombatParticipant> recipients, DamageType damageType, Action<EffectStrategy> finished)
        {
            if (recipients == null) { return; }

            float sign = Mathf.Sign(apChange);
            foreach (CombatParticipant recipient in recipients)
            {
                float modifiedAPChange = apChange + sign * UnityEngine.Random.Range(0f, jitter);
                modifiedAPChange += damageType switch
                {
                    DamageType.None => 0f,
                    DamageType.Physical => GetPhysicalModifier(sign, sender, recipient),
                    DamageType.Magical => GetMagicalModifier(sign, sender, recipient),
                    _ => 0f,
                };

                recipient.AdjustHP(modifiedAPChange);
            }

            finished?.Invoke(this);
        }
    }
}