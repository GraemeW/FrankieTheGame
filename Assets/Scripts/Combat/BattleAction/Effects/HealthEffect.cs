using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Combat
{
    [CreateAssetMenu(fileName = "New Health Effect", menuName = "BattleAction/Effects/Health Effect")]
    public class HealthEffect : EffectStrategy
    {
        [SerializeField] float healthChange = 0f;
        [Tooltip("Additional variability on base health change")][SerializeField][Min(0f)] float jitter = 0f;

        public override void StartEffect(CombatParticipant sender, IEnumerable<CombatParticipant> recipients, Action<EffectStrategy> finished)
        {
            if (recipients == null) { return; }

            foreach (CombatParticipant recipient in recipients)
            {
                float randomJitter = Mathf.Sign(healthChange) * UnityEngine.Random.Range(0f, jitter);
                recipient.AdjustHP(healthChange + randomJitter);
            }

            finished?.Invoke(this);
        }
    }
}
