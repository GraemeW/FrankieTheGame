using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Combat
{
    [CreateAssetMenu(fileName = "New Set Target Cooldown Effect", menuName = "BattleAction/Effects/Set Target Cooldown Effect")]
    public class SetTargetCooldownEffect : EffectStrategy
    {
        [SerializeField] private float cooldown = 8f;

        public override IEnumerator StartEffect(CombatParticipant sender, IList<BattleEntity> recipients, DamageType damageType)
        {
            if (recipients == null) { yield break; }

            foreach (BattleEntity recipient in recipients)
            {
                recipient.combatParticipant.SetCooldown(cooldown);
            }
        }
    }
}
