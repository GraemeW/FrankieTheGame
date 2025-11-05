using UnityEngine;

namespace Frankie.Stats
{
    public static class CalculatedStats
    {
        // Note:  Equations  have a lot of magic numbers for shaping
        private const float _cooldownMultiplierMin = 0.2f;
        private const float _cooldownMultiplierMax = 4f;
        private const float _hitChanceMin = 0.2f;
        private const float _hitChanceMax = 1.0f;
        private const float _critChanceMax = 0.5f;
        private const float _moveSpeedMin = 1.0f;

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
                CalculatedStat.MoveSpeed => Stat.Nimble,
                CalculatedStat.RunSpeed => Stat.Pluck,
                CalculatedStat.RunChance => Stat.Pluck,
                CalculatedStat.Fearsome => Stat.Pluck,
                CalculatedStat.Imposing => Stat.Pluck,
                _ => Stat.InitialLevel,
            };
            return stat != Stat.InitialLevel; // failsafe default behaviour
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
                CalculatedStat.MoveSpeed => GetMoveSpeed(level, callerModifier),
                CalculatedStat.RunSpeed => GetRunSpeed(callerModifier),
                CalculatedStat.RunChance => GetRunChance(callerModifier, contestModifier),
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
                , _cooldownMultiplierMin, _cooldownMultiplierMax);
        }

        private static float GetHitChance(float attackerModifier, float defenderModifier)
        {
            float deltaModifier = attackerModifier - defenderModifier;
            return Mathf.Clamp(
                0.85f + 0.15f * Mathf.Atan((deltaModifier + 8) / 8) / Mathf.PI,
                _hitChanceMin, _hitChanceMax);
        }

        private static float GetCritChance(float attackerModifier, float defenderModifier)
        {
            float deltaModifier = attackerModifier - defenderModifier;
            return _critChanceMax * (0.5f + Mathf.Atan((deltaModifier - 20) / 10) / Mathf.PI);
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

        private static float GetMoveSpeed(int level, float modifier)
        {
            float ratioModifier = 1 / (level * 15f);
            return Mathf.Max(_moveSpeedMin, modifier * ratioModifier);
        }

        private static float GetRunSpeed(float modifier)
        {
            return Mathf.Max(0f, modifier);
        }

        private static float GetRunChance(float modifier, float contestModifier)
        {
            float deltaModifier = modifier - contestModifier;
            return 0.5f + Mathf.Atan(deltaModifier / 20) / Mathf.PI;
        }

        private static float GetFearsome(float modifier, float defenderModifier)
        {
            // positive value = fearsome (multiplier* more than defender)
            return modifier - 3 * defenderModifier;
        }

        private static float GetImposing(float modifier, float defenderModifier)
        {
            // positive value -> imposing (multiplier* more than defender)
            return modifier - 6 * defenderModifier;
        }
        #endregion
    }
}
