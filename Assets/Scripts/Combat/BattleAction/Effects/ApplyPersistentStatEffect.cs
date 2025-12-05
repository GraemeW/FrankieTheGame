using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Frankie.Stats;

namespace Frankie.Combat
{
    [CreateAssetMenu(fileName = "New Persistent Stat Effect", menuName = "BattleAction/Effects/Persistent Stat Effect")]
    public class ApplyPersistentStatEffect : EffectStrategy, IPersistentStatusApplier
    {
        [SerializeField][Tooltip("Used if statContest is false")][Range(0, 1)] private float fractionProbabilityToApply = 0.5f;
        [SerializeField][Tooltip("Alternate probability derivation via stat contest")] private bool useStatContestProbabilityToApply = false;
        [SerializeField][Tooltip("Used if statContest is true")] private Stat statForContest = Stat.Luck;
        [SerializeField] private float duration = 10f;
        [SerializeField] private Stat stat = Stat.HP;
        [SerializeField] private float value = 1f;
        [SerializeField] private bool persistAfterCombat = false;
        [SerializeField] private int numberOfDuplicateEffectsAllowed = 3;

        public override IEnumerator StartEffect(CombatParticipant sender, IList<BattleEntity> recipients, DamageType damageType)
        {
            if (BaseStats.GetNonModifyingStats().Contains(stat)) { yield break; }
            if (recipients == null) { yield break; }

            foreach (BattleEntity recipient in recipients)
            {
                if (CheckForEffect(recipient.combatParticipant)) { continue; }

                bool probabilityCheck = useStatContestProbabilityToApply
                    ? IPersistentStatusApplier.CheckProbabilityToApply(sender, recipient.combatParticipant, statForContest)
                    : IPersistentStatusApplier.CheckProbabilityToApply(fractionProbabilityToApply);
                if (!probabilityCheck) { continue; }

                PersistentStatus activeStatusEffect = Apply(sender, recipient.combatParticipant, damageType);
                if (activeStatusEffect== null) { continue; }

                recipient.combatParticipant.AnnounceStateUpdate(StateAlteredType.StatusEffectApplied, activeStatusEffect);
            }
        }

        public bool CheckForEffect(CombatParticipant recipient)
        {
            return PersistentStatus.DoesEffectExist(recipient, effectGUID, numberOfDuplicateEffectsAllowed, duration);
        }

        public PersistentStatus Apply(CombatParticipant sender, CombatParticipant recipient, DamageType damageType)
        {
            PersistentStatModifierStatus activeStatusEffect = recipient.gameObject.AddComponent(typeof(PersistentStatModifierStatus)) as PersistentStatModifierStatus;
            if (activeStatusEffect == null) { return null; }
            activeStatusEffect.Setup(effectGUID, duration, stat, value, persistAfterCombat);
            return activeStatusEffect;
        }
    }
}
