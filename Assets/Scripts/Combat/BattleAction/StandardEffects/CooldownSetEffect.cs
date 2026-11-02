using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Combat
{
    [CreateAssetMenu(fileName = "New Cooldown Set Effect", menuName = "BattleAction/Effects/Cooldown Set Effect", order = 15)]
    public class CooldownSetEffect : EffectStrategy
    {
        // Tunables
        [SerializeField] private float cooldown = 8f;

        // Public Methods
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
