using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Stats
{
    public static class CalculatedStats
    {
        static float cooldownPreMultiplier = 1f;

        public static bool GetStatModifier(CalculatedStat calculatedStat, out Stat stat)
        {
            if (calculatedStat == CalculatedStat.CooldownFraction) { stat = Stat.Nimble; return true; }

            stat = Stat.EffectiveLevel;
            return false;
        }

        public static float GetCalculatedStat(CalculatedStat calculatedStat, int level, float modifier)
        {
            if (calculatedStat == CalculatedStat.CooldownFraction) { return GetCooldownFraction(level, modifier); }
            return 0f;
        }

        private static float GetCooldownFraction(int level, float nimble)
        {
            if (level < 1) { level = 1; }

            return cooldownPreMultiplier / (Mathf.PI / 2 + Mathf.Atan(nimble / level));
        }
    }
}
