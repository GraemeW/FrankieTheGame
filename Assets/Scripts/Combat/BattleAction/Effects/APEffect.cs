using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Combat
{
    [CreateAssetMenu(fileName = "New AP Effect", menuName = "BattleAction/Effects/AP Effect")]
    public class APEffect : EffectStrategy
    {
        [SerializeField] private float apChange;
        [Tooltip("Additional variability on base AP change")][SerializeField][Min(0f)] private float jitter;

        public override IEnumerator StartEffect(CombatParticipant sender, IList<BattleEntity> recipients, DamageType damageType)
        {
            if (recipients == null) { yield break; }

            float sign = Mathf.Sign(apChange);
            foreach (BattleEntity recipient in recipients)
            {
                float modifiedAPChange = apChange + sign * UnityEngine.Random.Range(0f, jitter);
                modifiedAPChange += damageType switch
                {
                    DamageType.None => 0f,
                    DamageType.Physical => GetPhysicalModifier(sign, sender, recipient.combatParticipant),
                    DamageType.Magical => GetMagicalModifier(sign, sender, recipient.combatParticipant),
                    _ => 0f,
                };

                recipient.combatParticipant.AdjustAP(modifiedAPChange);
            }
        }
    }
}
