using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Core
{
    public interface IPredicateEvaluator
    {
        bool? Evaluate(string predicate, string[] parameters);

        // Extended in PredicateEvaluatorExtension -- default implementations not allowed in Interfaces on current Unity C# runtime
        string MatchToPredicatesTemplate();
    }
}