using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Stats
{
    public static class CalculatedStats
    {
        // Note:  Equations  have a lot of magic numbers for shaping
        // Hard limits provided as static tunables here
        static float cooldownMultiplierMin = 0.5f;
        static float cooldownMultiplierMax = 4f;
        static float hitChanceMin = 0.2f;
        static float hitChanceMax = 1.0f;
        static float critChanceMax = 0.5f;

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
                CalculatedStat.RunSpeed => Stat.Pluck,
                CalculatedStat.Fearsome => Stat.Pluck,
                CalculatedStat.Imposing => Stat.Pluck,
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
                CalculatedStat.RunSpeed => GetRunSpeed(callerModifier),
                CalculatedStat.Fearsome => GetFearsome(callerModifier, contestModifier),
                CalculatedStat.Imposing => GetImposing(callerModifier, contestModifier),
                _ => 0f,
            };
        }
        #endregion

        #region Calculations
        private static float GetCooldownFraction(int level, float modifier)
        {
            return Mathf.Clamp(
                1f / (0.5f + Mathf.Atan(modifier / (5* level))/Mathf.PI) - 1
                , cooldownMultiplierMin, cooldownMultiplierMax);
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
            return critChanceMax * (0.5f + Mathf.Atan((deltaModifier - 20) / 10) / Mathf.PI);
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

        private static float GetRunSpeed(float modifier)
        {
            return Mathf.Max(0f, modifier);
        }

        private static float GetFearsome(float modifier, float defenderModifier)
        {
            // positive value = fearsome (multiplier* more than defender)
            return modifier - 6 * defenderModifier;
        }

        private static float GetImposing(float modifier, float defenderModifier)
        {
            // positive value -> imposing (multiplier* more than defender)
            return modifier - 12 * defenderModifier;
        }
        #endregion
    }
}
