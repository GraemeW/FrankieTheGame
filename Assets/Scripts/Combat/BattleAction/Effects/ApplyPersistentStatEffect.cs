using Frankie.Stats;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Frankie.Combat
{
    [CreateAssetMenu(fileName = "New Persistent Stat Effect", menuName = "BattleAction/Effects/Persistent Stat Effect")]
    public class ApplyPersistentStatEffect : EffectStrategy
    {
        [SerializeField] [Range(0, 1)] float fractionProbabilityToApply = 0.5f;
        [SerializeField] float duration = 10f;
        [SerializeField] Stat stat = Stat.HP;
        [SerializeField] float value = 1f;
        [SerializeField] bool persistAfterCombat = false;

        public override void StartEffect(CombatParticipant sender, IEnumerable<BattleEntity> recipients, DamageType damageType, Action<EffectStrategy> finished)
        {
            if (BaseStats.GetNonModifyingStats().Contains(stat)) { return; }
            if (recipients == null) { return; }

            foreach (BattleEntity battleEntity in recipients)
            {
                float chanceRoll = UnityEngine.Random.Range(0f, 1f);
                if (fractionProbabilityToApply < chanceRoll) { return; }

                PersistentStatModifierStatus activeStatusEffect = battleEntity.combatParticipant.gameObject.AddComponent(typeof(PersistentStatModifierStatus)) as PersistentStatModifierStatus;
                activeStatusEffect.Setup(duration, stat, value, persistAfterCombat);

                battleEntity.combatParticipant.AnnounceStateUpdate(new StateAlteredData(StateAlteredType.StatusEffectApplied, activeStatusEffect));
            }

            finished?.Invoke(this);
        }
    }
}