using System.Collections.Generic;

namespace Frankie.Stats
{
    public interface IModifierProvider
    {
        IEnumerable<float> GetAdditiveModifiers(Stat stat);
    }
}
