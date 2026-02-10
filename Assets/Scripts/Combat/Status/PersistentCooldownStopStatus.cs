using Frankie.Stats;
using UnityEngine;

namespace Frankie.Combat
{
    [RequireComponent(typeof(CombatParticipant))]
    public class PersistentCooldownStopStatus : PersistentStatus
    {
        // State
        private bool isPaused = false;
        private float probabilityToRemoveWithDamage = 0.0f;
        private float previousCooldown = 0.0f;
        
        protected override void Awake()
        {
            base.Awake();
            
            // Only one cooldown stop should exist at a time
            foreach (PersistentCooldownStopStatus existingCooldownStatus in GetComponents<PersistentCooldownStopStatus> ())
            {
                if (existingCooldownStatus == this) { continue; }
                existingCooldownStatus.CancelEffect();
            }

            ToggleCooldownPause(true);
        }

        public void Setup(string setEffectGUID, float duration, float setProbabilityToRemoveWithDamage)
        {
            base.Setup(setEffectGUID, duration, false);
            probabilityToRemoveWithDamage = Mathf.Clamp(setProbabilityToRemoveWithDamage, 0.0f, 1.0f);
            statAffected = Stat.Nimble;
            isIncrease = false;
        }

        protected override void CancelEffect()
        {
            ToggleCooldownPause(false);
            base.CancelEffect();
        }
        
        protected override void HandleCombatState(StateAlteredInfo stateAlteredInfo)
        {
            base.HandleCombatState(stateAlteredInfo);
            if (stateAlteredInfo.stateAlteredType != StateAlteredType.CooldownSet) { return; }
            
            // In some cases an action is already queued, so re-pause after dequeue & cooldown set
            if (isPaused && !float.IsPositiveInfinity(stateAlteredInfo.points))
            {
                ToggleCooldownPause(true);
            }
        }

        protected override void OnDamage()
        {
            if (Mathf.Approximately(probabilityToRemoveWithDamage, 0f)) { return; }
            if (Mathf.Approximately(probabilityToRemoveWithDamage, 1.0f) || UnityEngine.Random.Range(0.0f, 1.0f) < probabilityToRemoveWithDamage)
            {
                CancelEffect();
            }
        }

        private void ToggleCooldownPause(bool enable)
        {
            isPaused = enable;
            if (enable)
            {
                previousCooldown = combatParticipant.GetCooldown();
                combatParticipant.PauseCooldown();
            }
            else
            {
                combatParticipant.SetCooldown(previousCooldown);
            }
        }
    }
}
