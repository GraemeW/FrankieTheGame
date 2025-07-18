using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Core
{
    public abstract class Predicate : ScriptableObject
    {
        // Generally implementing:
        //     public abstract bool? Evaluate(T evaluationInput);
        // Relevant moreso for IPredicateEvaluator implementations, which downselects to the child predicates
        // Avoiding generic typing at this level due to knock-on complexity/mess
    }
}
