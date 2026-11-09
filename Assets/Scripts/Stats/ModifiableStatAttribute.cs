using System;
using LowDefMustard.Utils;

namespace Frankie.Stats
{
    public class ModifiableStatAttribute : RestrictedEnumAttribute
    {
        public ModifiableStatAttribute() : base(Array.ConvertAll(BaseStats.GetNonModifyingStats(), stat => (int)stat)) { }
    }
}
