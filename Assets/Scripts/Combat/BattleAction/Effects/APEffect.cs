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

        public override void StartEffect(CombatParticipant sender, IEnumerable<CombatParticipant> recipients, Action<EffectStrategy> finished)
        {
            if (recipients == null) { return; }

            foreach (CombatParticipant recipient in recipients)
            {
                float randomJitter = Mathf.Sign(apChange) * UnityEngine.Random.Range(0f, jitter);
                recipient.AdjustAP(apChange + randomJitter);
            }

            finished?.Invoke(this);
        }
    }
}