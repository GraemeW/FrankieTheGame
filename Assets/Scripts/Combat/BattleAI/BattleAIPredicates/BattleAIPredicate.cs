using Frankie.Core;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Combat
{
    public abstract class BattleAIPredicate : Predicate
    {
        public abstract bool? Evaluate(BattleAI battleAI);
    }
}
