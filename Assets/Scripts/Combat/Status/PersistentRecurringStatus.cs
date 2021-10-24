using Frankie.Control;
using Frankie.Stats;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

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

        public void Setup(StatusType statusEffectType, float duration, float tickPeriod, Action recurringEffect, bool persistAfterBattle = false)
        {
            if (recurringEffect == null) { Destroy(this); }

            base.Setup(statusEffectType, duration, persistAfterBattle);
            this.tickPeriod = tickPeriod;
            this.recurringEffect = recurringEffect;
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
