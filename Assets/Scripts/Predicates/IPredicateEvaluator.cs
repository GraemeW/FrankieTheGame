using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Core
{
    public interface IPredicateEvaluator
    {
        bool? Evaluate(Predicate predicate);
    }
}