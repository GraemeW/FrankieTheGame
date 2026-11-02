using System;
using UnityEngine;

namespace LowDefMustard.Rendering.Editor
{
    public readonly struct BattleEffectPreviewSize : IEquatable<BattleEffectPreviewSize>
    {
        // Note:  Equal by name only (a name is listed once)

        public string name { get; }
        public Vector2 size { get; }

        public BattleEffectPreviewSize(string name, Vector2 size)
        {
            this.name = name;
            this.size = size;
        }

        public bool Equals(BattleEffectPreviewSize other) => string.Equals(name, other.name, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is BattleEffectPreviewSize other && Equals(other);
        public override int GetHashCode() => name != null ? StringComparer.Ordinal.GetHashCode(name) : 0;
        public static bool operator ==(BattleEffectPreviewSize left, BattleEffectPreviewSize right) => left.Equals(right);
        public static bool operator !=(BattleEffectPreviewSize left, BattleEffectPreviewSize right) => !left.Equals(right);
    }
}
