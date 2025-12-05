using System;
using UnityEngine;
using Frankie.Stats;

namespace Frankie.Combat
{
    [RequireComponent(typeof(CombatParticipant))]
    public class PersistentRecurringStatus : PersistentStatus
    {
        // Tunables
        private float tickPeriod = 5f;
        private Action recurringEffect;

        // State
        private float tickTimer;
        private bool queuedTick = false;
        
        protected override void Update()
        {
            base.Update();
            HandleRecurringEffects();
        }
        
        public void Setup(string setEffectGUID, float duration, float setTickPeriod, Action setRecurringEffect, Stat setStatAffected, bool setIsIncreasing, bool persistAfterBattle = false)
        {
            if (setRecurringEffect == null) { CancelEffect(); }

            base.Setup(setEffectGUID, duration, persistAfterBattle);
            tickPeriod = setTickPeriod;
            recurringEffect = setRecurringEffect;
            statAffected = setStatAffected;
            isIncrease = setIsIncreasing;
        }

        protected override void UpdateTimers()
        {
            base.UpdateTimers();
            queuedTick = false;
            tickTimer += Time.deltaTime;

            if (tickTimer > tickPeriod)
            {
                queuedTick = true;
                tickTimer = 0f;
            }
        }

        private void HandleRecurringEffects()
        {
            if (recurringEffect == null || !active) { return; }
            if (queuedTick) { recurringEffect.Invoke(); }
        }
    }
}
