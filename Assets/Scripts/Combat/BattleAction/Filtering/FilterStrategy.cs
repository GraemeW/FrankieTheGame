using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Combat
{
    public abstract class FilterStrategy : ScriptableObject
    {
        // Abstract methods
        public abstract IEnumerable<BattleEntity> Filter(IEnumerable<BattleEntity> objectsToFilter);
    }
}
