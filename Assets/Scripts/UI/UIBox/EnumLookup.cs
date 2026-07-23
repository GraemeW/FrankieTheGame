using System;
using System.Collections.Generic;

namespace Frankie.Utils.UI
{
    public class EnumLookup<TEnum, TValue> : EnumLookupBase<TValue> where TEnum : struct, Enum
    {
        private readonly Dictionary<TEnum, TValue> values =  new();
        private TValue Get(TEnum key) => values.GetValueOrDefault(key);
        private void Set(TEnum key, TValue value) => values[key] = value;
        public override bool TryGet(Enum key, out TValue output)
        {
            output = default;
            if (key is not TEnum castKey) { return false; }
            output = Get(castKey);
            return output != null;
        }

        public override bool TrySet(Enum key, TValue input)
        {
            if (key is not TEnum castKey) { return false; }
            Set(castKey, input);
            return true;
        }
    }
}
