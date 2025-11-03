using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Frankie.Stats;

namespace Frankie.Combat
{
    [CreateAssetMenu(fileName = "New Permanent Stat Effect", menuName = "BattleAction/Effects/Permanent Stat Effect")]
    public class ApplyPermanentStatEffect : EffectStrategy
    {
        [SerializeField] private Stat stat = Stat.HP;
        [SerializeField] private float value = 1f;

        public override IEnumerator StartEffect(CombatParticipant sender, IList<BattleEntity> recipients, DamageType damageType)
        {
            if (BaseStats.GetNonModifyingStats().Contains(stat)) { yield break; }
            if (recipients == null) { yield break; }

            foreach (BattleEntity battleEntity in recipients)
            {
                if (!battleEntity.combatParticipant.TryGetComponent(out BaseStats baseStats)) { continue; }
                baseStats.AdjustStat(stat, value);

                battleEntity.combatParticipant.AnnounceStateUpdate(StateAlteredType.StatusEffectApplied, stat, value);
            }
        }
    }
}
