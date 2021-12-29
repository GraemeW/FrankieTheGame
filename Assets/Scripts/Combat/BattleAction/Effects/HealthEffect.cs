using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Combat
{
    [CreateAssetMenu(fileName = "New Health Effect", menuName = "BattleAction/Effects/Health Effect")]
    public class HealthEffect : EffectStrategy
    {
        [Tooltip("Effective minimum change")][SerializeField] float healthChange = 0f;
        [Tooltip("Added on top as range (0 to jitter), sign based on health change")][SerializeField][Min(0f)] float jitter = 0f;
        [SerializeField] bool canMiss = true;
        [SerializeField] bool canCrit = true;
        [SerializeField][Min(1f)] float critMultiplier = 2f;

        public override void StartEffect(CombatParticipant sender, IEnumerable<CombatParticipant> recipients, Action<EffectStrategy> finished)
        {
            if (recipients == null) { return; }

            foreach (CombatParticipant recipient in recipients)
            {
                if (!DoesAttackHit(canMiss, sender, recipient)) { continue; }

                float modifiedHealthChange = healthChange + Mathf.Sign(healthChange) * UnityEngine.Random.Range(0f, jitter);
                modifiedHealthChange *= GetCritModifier(canCrit, critMultiplier, sender, recipient);

                recipient.AdjustHP(modifiedHealthChange);
            }

            finished?.Invoke(this);
        }
    }
}
