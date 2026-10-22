using UnityEngine;

namespace LowDefMustard.Rendering
{
    internal readonly struct BattleEffectTintSettings
    {
        public BattleEffectTintMode mode { get; }
        public float angleRadians { get; }
        public float frequency { get; }
        public float speed { get; }

        public BattleEffectTintSettings(BattleEffectTintMode mode, float angleRadians, float frequency, float speed)
        {
            this.mode = mode;
            this.angleRadians = angleRadians;
            this.frequency = frequency;
            this.speed = speed;
        }

        public Vector4 ToVector4() => new((int)mode, angleRadians, frequency, speed);
    }
}
