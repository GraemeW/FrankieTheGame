using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Frankie.Stats;

namespace Frankie.Combat
{
    [CreateAssetMenu(fileName = "New Persistent Stat Effect", menuName = "BattleAction/Effects/Persistent Stat Effect")]
    public class ApplyPersistentStatEffect : EffectStrategy
    {
        [SerializeField][Range(0, 1)] private float fractionProbabilityToApply = 0.5f;
        [SerializeField] private float duration = 10f;
        [SerializeField] private Stat stat = Stat.HP;
        [SerializeField] private float value = 1f;
        [SerializeField] private bool persistAfterCombat = false;

        public override IEnumerator StartEffect(CombatParticipant sender, IList<BattleEntity> recipients, DamageType damageType)
        {
            if (BaseStats.GetNonModifyingStats().Contains(stat)) { yield break; }
            if (recipients == null) { yield break; }

            foreach (BattleEntity battleEntity in recipients)
            {
                float chanceRoll = UnityEngine.Random.Range(0f, 1f);
                if (fractionProbabilityToApply < chanceRoll) { continue; }

                PersistentStatModifierStatus activeStatusEffect = battleEntity.combatParticipant.gameObject.AddComponent(typeof(PersistentStatModifierStatus)) as PersistentStatModifierStatus;
                if (activeStatusEffect == null) { continue; }
                activeStatusEffect.Setup(duration, stat, value, persistAfterCombat);

                battleEntity.combatParticipant.AnnounceStateUpdate(StateAlteredType.StatusEffectApplied, activeStatusEffect);
            }
        }
    }
}
