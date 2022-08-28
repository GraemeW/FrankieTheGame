using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Combat
{
    public abstract class FilterStrategy : ScriptableObject
    {
        public abstract IEnumerable<BattleEntity> Filter(IEnumerable<BattleEntity> objectsToFilter);
    }
}