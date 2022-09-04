using Frankie.Speech;
using Frankie.Stats;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Core
{
    public abstract class PredicateAIConversant : Predicate
    {
        public abstract bool? Evaluate(AIConversant aiConversant);
    }
}

