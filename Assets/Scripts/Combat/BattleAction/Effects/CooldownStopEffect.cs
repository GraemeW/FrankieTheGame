using System.Collections;
using System.Collections.Generic;
using Frankie.Stats;
using UnityEngine;

namespace Frankie.Combat
{
    [CreateAssetMenu(fileName = "New Cooldown Stop Effect", menuName = "BattleAction/Effects/Cooldown Stop Effect")]
    public class CooldownStopEffect : EffectStrategy, IPersistentStatusApplier
    {
        // Tunables
        [SerializeField][Tooltip("Used if statContest is false")][Range(0, 1)] private float fractionProbabilityToApply = 0.5f;
        [SerializeField][Tooltip("Alternate probability derivation via stat contest")] private bool useStatContestProbabilityToApply = false;
        [SerializeField][Tooltip("Used if statContest is true")] private Stat statForContest = Stat.Luck;
        [SerializeField] private float duration = 10f;
        [SerializeField][Range(0, 1)] private float probabilityToRemoveWithDamage = 0.5f;
        
        // Fixed
        const int _numberOfDuplicateEffectsAllowed = 1;
        
        public override IEnumerator StartEffect(CombatParticipant sender, IList<BattleEntity> recipients, DamageType damageType)
        {
            if (recipients == null) { yield break; }

            foreach (BattleEntity recipient in recipients)
            {
                if (CheckForEffect(recipient.combatParticipant)) { continue; }

                bool probabilityCheck = useStatContestProbabilityToApply
                    ? IPersistentStatusApplier.CheckProbabilityToApply(sender, recipient.combatParticipant, statForContest)
                    : IPersistentStatusApplier.CheckProbabilityToApply(fractionProbabilityToApply);
                if (!probabilityCheck) { continue; }

                PersistentStatus activeStatusEffect = Apply(sender, recipient.combatParticipant, damageType);
                if (activeStatusEffect == null) { continue; }

                recipient.combatParticipant.AnnounceStateUpdate(StateAlteredType.StatusEffectApplied, activeStatusEffect);
            }
        }
        
        public bool CheckForEffect(CombatParticipant recipient)
        {
            return PersistentStatus.DoesEffectExist(recipient, effectGUID, _numberOfDuplicateEffectsAllowed, duration);
        }

        public PersistentStatus Apply(CombatParticipant sender, CombatParticipant recipient, DamageType damageType)
        {
            PersistentCooldownStopStatus activeStatusEffect = recipient.gameObject.AddComponent(typeof(PersistentCooldownStopStatus)) as PersistentCooldownStopStatus;
            if (activeStatusEffect == null) { return null; }
            activeStatusEffect.Setup(effectGUID, duration, probabilityToRemoveWithDamage);
            return activeStatusEffect;
        }
    }
}
