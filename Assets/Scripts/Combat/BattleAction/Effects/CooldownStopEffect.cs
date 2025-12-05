using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Combat
{
    [CreateAssetMenu(fileName = "New Cooldown Stop Effect", menuName = "BattleAction/Effects/Cooldown Stop Effect")]
    public class CooldownStopEffect : EffectStrategy
    {
        // Tunables
        [SerializeField][Range(0, 1)] private float probabilityToRemoveWithDamage = 0.5f;
        [SerializeField][Range(0, 1)] private float fractionProbabilityToApply = 0.5f;
        [SerializeField] private float duration = 10f;
        
        // Fixed
        const int _numberOfDuplicateEffectsAllowed = 1;
        
        public override IEnumerator StartEffect(CombatParticipant sender, IList<BattleEntity> recipients, DamageType damageType)
        {
            if (recipients == null) { yield break; }

            foreach (BattleEntity recipient in recipients)
            {
                float chanceRoll = UnityEngine.Random.Range(0f, 1f);
                if (fractionProbabilityToApply < chanceRoll) { continue; }
                
                if (PersistentStatus.DoesEffectExist(recipient, effectGUID, _numberOfDuplicateEffectsAllowed, duration)) continue;

                PersistentCooldownStopStatus activeStatusEffect = recipient.combatParticipant.gameObject.AddComponent(typeof(PersistentCooldownStopStatus)) as PersistentCooldownStopStatus;
                if (activeStatusEffect == null) { continue; }
                activeStatusEffect.Setup(effectGUID, duration, probabilityToRemoveWithDamage);

                recipient.combatParticipant.AnnounceStateUpdate(StateAlteredType.StatusEffectApplied, activeStatusEffect);
            }
        }
    }
}
