using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Combat
{
    [CreateAssetMenu(fileName = "New Set Target Cooldown Effect", menuName = "BattleAction/Effects/Set Target Cooldown Effect")]
    public class SetTargetCooldownEffect : EffectStrategy
    {
        [SerializeField] float cooldown = 8f;

        public override void StartEffect(CombatParticipant sender, IEnumerable<BattleEntity> recipients, DamageType damageType, Action<EffectStrategy> finished)
        {
            if (recipients == null) { return; }

            foreach (BattleEntity recipient in recipients)
            {
                recipient.combatParticipant.SetCooldown(cooldown);
            }

            finished?.Invoke(this);
        }
    }
}