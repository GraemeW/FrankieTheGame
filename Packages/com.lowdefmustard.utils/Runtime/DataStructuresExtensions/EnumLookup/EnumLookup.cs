using System;
using System.Collections.Generic;

namespace LowDefMustard.Utils
{
    public class EnumLookup<TEnum, TValue> : EnumLookupBase<TValue> where TEnum : struct, Enum
    {
        private readonly Dictionary<TEnum, TValue> values =  new();
        private void Set(TEnum key, TValue value) => values[key] = value;
        public override bool TryGet(Enum key, out TValue output)
        {
            output = default;
            return key is TEnum castKey && values.TryGetValue(castKey, out output);
        }

        public override bool TrySet(Enum key, TValue input)
        {
            if (key is not TEnum castKey) { return false; }
            Set(castKey, input);
            return true;
        }
    }
}
