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
        [SerializeField] StatusType statusEffectType = default;
        [SerializeField] float duration = 10f;
        [SerializeField] Stat stat = Stat.HP;
        [SerializeField] float value = 1f;
        [SerializeField] bool persistAfterCombat = false;

        public override void StartEffect(CombatParticipant sender, IEnumerable<CombatParticipant> recipients, Action<EffectStrategy> finished)
        {
            if (BaseStats.GetNonModifyingStats().Contains(stat)) { return; }
            if (recipients == null) { return; }

            foreach (CombatParticipant combatParticipant in recipients)
            {
                float chanceRoll = UnityEngine.Random.Range(0f, 1f);
                if (fractionProbabilityToApply < chanceRoll) { return; }

                PersistentStatModifierStatus activeStatusEffect = combatParticipant.gameObject.AddComponent(typeof(PersistentStatModifierStatus)) as PersistentStatModifierStatus;
                activeStatusEffect.Setup(statusEffectType, duration, stat, value, persistAfterCombat);

                combatParticipant.AnnounceStateUpdate(new StateAlteredData(StateAlteredType.StatusEffectApplied, statusEffectType));
            }

            finished?.Invoke(this);
        }
    }
}