using Frankie.Stats;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Frankie.Combat
{
    [CreateAssetMenu(fileName = "New Permanent Stat Effect", menuName = "BattleAction/Effects/Permanent Stat Effect")]
    public class ApplyPermanentStatEffect : EffectStrategy
    {
        [SerializeField] Stat stat = Stat.HP;
        [SerializeField] float value = 1f;

        public override void StartEffect(CombatParticipant sender, IEnumerable<BattleEntity> recipients, DamageType damageType, Action<EffectStrategy> finished)
        {
            if (BaseStats.GetNonModifyingStats().Contains(stat)) { return; }
            if (recipients == null) { return; }

            foreach (BattleEntity battleEntity in recipients)
            {
                if (!battleEntity.combatParticipant.TryGetComponent(out BaseStats baseStats)) { continue; }
                baseStats.AdjustStat(stat, value);

                battleEntity.combatParticipant.AnnounceStateUpdate(StateAlteredType.StatusEffectApplied, stat, value);
            }

            finished?.Invoke(this);
        }
    }
}