using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LowDefMustard.Utils
{
    [System.Serializable]
    public class Condition
    {
        [NonReorderable] [SerializeField] private List<Disjunction> and = new();

        public bool Check(IEnumerable<IPredicateEvaluator> evaluators)
        {
            return and.All(disjunction => disjunction.Check(evaluators));
        }

        public void AddDisjunction(Disjunction disjunction) => and.Add(disjunction);
        public IReadOnlyList<Disjunction> GetDisjunctions() => and;

        [System.Serializable]
        public class Disjunction
        {
            [NonReorderable] [SerializeField] private List<PredicateWrapper> or = new();

            public bool Check(IEnumerable<IPredicateEvaluator> evaluators)
            {
                return or.Any(predicateWrapper => predicateWrapper.Check(evaluators));
            }

            public void AddPredicateWrapper(PredicateWrapper predicateWrapper) => or.Add(predicateWrapper);
            public IReadOnlyList<PredicateWrapper> GetPredicateWrappers() => or;
        }

        [System.Serializable]
        public class PredicateWrapper
        {
            [SerializeField] private Predicate predicate;
            [SerializeField] private bool negate;

            // Parameterless constructor kept for Unity's serializer (Inspector-created entries still get default field values)
            public PredicateWrapper()
            {
            }

            public PredicateWrapper(Predicate predicate, bool negate = false)
            {
                this.predicate = predicate;
                this.negate = negate;
            }

            public bool Check(IEnumerable<IPredicateEvaluator> evaluators)
            {
                if (predicate == null) { return true; }
                return evaluators.Select(evaluator => evaluator.Evaluate(predicate)).All(result => result != negate);
            }

            public Predicate GetPredicate() => predicate;
            public bool GetNegate() => negate;
        }
    }
}
