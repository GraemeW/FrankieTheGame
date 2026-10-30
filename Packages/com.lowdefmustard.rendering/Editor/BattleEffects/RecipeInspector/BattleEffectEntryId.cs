#if UNITY_EDITOR
using System;

namespace LowDefMustard.Rendering.Editor
{
    internal readonly struct BattleEffectEntryId : IEquatable<BattleEffectEntryId>
    {
        public int index { get; }
        public bool isInstance { get; }

        public BattleEffectEntryId(int index, bool isInstance)
        {
            this.index = index;
            this.isInstance = isInstance;
        }

        public static BattleEffectEntryId Item(int index) => new(index, false);
        public static BattleEffectEntryId Instance(int index) => new(index, true);

        public bool Equals(BattleEffectEntryId other) => index == other.index && isInstance == other.isInstance;
        public override bool Equals(object obj) => obj is BattleEffectEntryId other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(index, isInstance);
        public override string ToString() => $"{(isInstance ? "Instance" : "Item")} {index}";
    }
}
#endif
