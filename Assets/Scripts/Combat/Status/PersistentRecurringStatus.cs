using System;
using System.Linq;
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
        
        #region StaticMethods
        public static bool DoesEffectExist(BattleEntity recipient, string effectGUID, int threshold, float resetDurationOnDupe = 0f)
        {
            int duplicateEffectCount = 0;
            PersistentRecurringStatus minimumDurationStatusEffect = null;
            foreach (PersistentRecurringStatus existingStatusEffect in recipient.combatParticipant.GetComponents<PersistentRecurringStatus>().Where(x => x.GetEffectGUID() == effectGUID).OrderBy(x => x.GetDuration()))
            {
                duplicateEffectCount++;
                
                if (duplicateEffectCount == 1) { minimumDurationStatusEffect = existingStatusEffect; }
                if (duplicateEffectCount >= threshold) { break; }
            }
            if (duplicateEffectCount < threshold) return false;
            
            if (minimumDurationStatusEffect != null) { minimumDurationStatusEffect.ResetDuration(resetDurationOnDupe); }
            return true;
        }
        #endregion
        
        #region UnityMethods
        protected override void Update()
        {
            base.Update();
            HandleRecurringEffects();
        }
        #endregion

        #region ClassMethods
        public void Setup(string setEffectGUID, float duration, float setTickPeriod, Action setRecurringEffect, Stat setStatAffected, bool setIsIncreasing, bool persistAfterBattle = false)
        {
            if (setRecurringEffect == null) { Destroy(this); }

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
        #endregion
    }
}
