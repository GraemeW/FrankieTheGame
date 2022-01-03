using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Stats
{
    public static class CalculatedStats
    {
        // Note:  Equations  have a lot of magic numbers for shaping
        // Hard limits provided as static tunables here
        static float cooldownMax = 10f;
        static float hitChanceMin = 0.2f;
        static float hitChanceMax = 1.0f;
        static float critChanceMin = 0f;
        static float critChanceMax = 1.0f;

        #region Getters
        public static bool GetStatModifier(CalculatedStat calculatedStat, out Stat stat)
        {
            stat = calculatedStat switch
            {
                CalculatedStat.CooldownFraction => Stat.Pluck,
                CalculatedStat.HitChance => Stat.Luck,
                CalculatedStat.CritChance => Stat.Pluck,
                CalculatedStat.PhysicalAdder => Stat.Brawn,
                CalculatedStat.MagicalAdder => Stat.Beauty,
                CalculatedStat.Defense => Stat.Nimble,
                _ => Stat.ExperienceReward,
            };

            if (stat == Stat.ExperienceReward) { return false; } // failsafe default behaviour
            return true;
        }

        public static float GetCalculatedStat(CalculatedStat calculatedStat, int level, float callerModifier, float contestModifier = 0f)
        {
            if (level < 1) { level = 1; }

            return calculatedStat switch
            {
                CalculatedStat.CooldownFraction => GetCooldownFraction(level, callerModifier),
                CalculatedStat.HitChance => GetHitChance(callerModifier, contestModifier),
                CalculatedStat.CritChance => GetCritChance(callerModifier, contestModifier),
                CalculatedStat.PhysicalAdder => GetPhysicalAdder(callerModifier),
                CalculatedStat.MagicalAdder => GetMagicalAdder(callerModifier),
                CalculatedStat.Defense => GetDefense(callerModifier),
                _ => 0f,
            };
        }
        #endregion

        #region Calculations
        private static float GetCooldownFraction(int level, float modifier)
        {
            return Mathf.Min(
                1f / (0.5f + Mathf.Atan(modifier / (5* level))/Mathf.PI) - 1
                , cooldownMax);
        }

        private static float GetHitChance(float attackerModifier, float defenderModifier)
        {
            float deltaModifier = attackerModifier - defenderModifier;
            return Mathf.Clamp(
                0.5f + Mathf.Atan((deltaModifier + 8) / 8) / Mathf.PI,
                hitChanceMin, hitChanceMax);
        }

        private static float GetCritChance(float attackerModifier, float defenderModifier)
        {
            float deltaModifier = attackerModifier - defenderModifier;
            return Mathf.Clamp(
                0.5f + Mathf.Atan((deltaModifier - 20) / 10) / Mathf.PI,
                critChanceMin, critChanceMax);
        }

        private static float GetPhysicalAdder(float modifier)
        {
            return Mathf.Max(0f, modifier);
        }

        private static float GetMagicalAdder(float modifier)
        {
            return Mathf.Max(0f, modifier);
        }

        private static float GetDefense(float modifier)
        {
            return Mathf.Max(0f, modifier);
        }
        #endregion
    }
}
