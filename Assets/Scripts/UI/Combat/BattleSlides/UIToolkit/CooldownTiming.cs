using System;
using UnityEngine;

namespace Frankie.Combat.UI
{
    public readonly struct CooldownTiming : IEquatable<CooldownTiming>
    {
        // Const Tunables
        private const float _refillDuration = 1f; // Non-positive cooldowns (e.g. expiry) refill over this time

        // State
        public readonly float startTime;
        public readonly float duration;
        public readonly bool isPaused;

        private CooldownTiming(float startTime, float duration, bool isPaused)
        {
            this.startTime = startTime;
            this.duration = duration;
            this.isPaused = isPaused;
        }

        public static CooldownTiming Restart(float cooldownDuration)
        {
            bool isPaused = float.IsPositiveInfinity(cooldownDuration);
            return new CooldownTiming(Time.time, cooldownDuration > 0f ? cooldownDuration : _refillDuration, isPaused);
        }

        public float GetFraction(float time) => duration <= 0f ? 1f : Mathf.Clamp01((time - startTime) / duration);

        public bool Equals(CooldownTiming other) => startTime.Equals(other.startTime) && duration.Equals(other.duration) && isPaused == other.isPaused;
        public override bool Equals(object obj) => obj is CooldownTiming other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(startTime, duration, isPaused);
    }
}
