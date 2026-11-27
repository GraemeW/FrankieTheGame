using System;
using System.Collections.Generic;

namespace LowDefMustard.Utils
{
    internal readonly struct HandleKey<TSlot> : IEquatable<HandleKey<TSlot>> where TSlot : struct, Enum
    {
        public readonly string ownerKey;
        public readonly TSlot slot;

        public HandleKey(string ownerKey, TSlot slot)
        {
            this.ownerKey = ownerKey;
            this.slot = slot;
        }

        public bool Equals(HandleKey<TSlot> other) => ownerKey == other.ownerKey && EqualityComparer<TSlot>.Default.Equals(slot, other.slot);
        public override bool Equals(object obj) => obj is HandleKey<TSlot> other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(ownerKey, slot);
    }
}
