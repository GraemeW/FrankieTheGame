using System;
using UnityEngine;
using Frankie.Stats;

namespace Frankie.Combat
{
    [RequireComponent(typeof(CombatParticipant))]
    public class PersistentRecurringStatus : PersistentStatus
    {
        // Tunables
        float tickPeriod = 5f;
        Action recurringEffect = null;

        // State
        float tickTimer = 0f;
        bool queuedTick = false;

        protected override void Update()
        {
            base.Update();
            HandleRecurringEffects();
        }

        public void Setup(float duration, float tickPeriod, Action recurringEffect, Stat statAffected, bool isIncrease, bool persistAfterBattle = false)
        {
            if (recurringEffect == null) { Destroy(this); }

            base.Setup(duration, persistAfterBattle);
            this.tickPeriod = tickPeriod;
            this.recurringEffect = recurringEffect;
            this.statusEffectType = statAffected;
            this.isIncrease = isIncrease;
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

            if (queuedTick)
            {
                recurringEffect.Invoke();
            }
        }
    }
}
