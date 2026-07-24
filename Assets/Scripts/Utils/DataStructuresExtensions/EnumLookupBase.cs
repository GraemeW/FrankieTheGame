using System;
using System.Collections.Generic;

namespace Frankie.Utils
{
    public abstract class EnumLookupBase<TValue>
    {
        public abstract bool TryGet(Enum key, out TValue output);
        public abstract bool TrySet(Enum key, TValue input);

        public IEnumerable<TValue> GetValues<T>() where T : struct, Enum
        {
            foreach (Enum checkEnum in Enum.GetValues(typeof(T)))
            {
                if (!TryGet(checkEnum, out TValue value)) { continue; }
                yield return value;
            }
        }
    }
}
