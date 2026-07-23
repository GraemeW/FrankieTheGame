using System;

namespace Frankie.Utils.UI
{
    public abstract class EnumLookupBase<TOutput>
    {
        public abstract bool TryGet(Enum key, out TOutput output);
        public abstract bool TrySet(Enum key, TOutput input);
    }
}
