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
                CalculatedStat.CooldownFraction => Stat.Nimble,
                CalculatedStat.HitChance => Stat.Luck,
                CalculatedStat.CritChance => Stat.Pluck,
                _ => Stat.ExperienceReward,
            };

            if (stat == Stat.ExperienceReward) { return false; } // failsafe default behaviour
            return true;
        }

        public static float GetCalculatedStat(CalculatedStat calculatedStat, int level, float attackerModifier, float defenderModifier = 0f)
        {
            if (level < 1) { level = 1; }

            return calculatedStat switch
            {
                CalculatedStat.CooldownFraction => GetCooldownFraction(level, attackerModifier),
                CalculatedStat.HitChance => GetHitChance(attackerModifier, defenderModifier),
                CalculatedStat.CritChance => GetCritChance(attackerModifier, defenderModifier),
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
                0.5f + Mathf.Atan((deltaModifier - 8) / 6) / Mathf.PI,
                critChanceMin, critChanceMax);
        }
        #endregion
    }
}
