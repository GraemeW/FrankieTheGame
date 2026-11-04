using System.Collections.Generic;
using System.Linq;

namespace LowDefMustard.Utils.Tests
{
    // Shorthand test convenience condition builder functions
    //  - When / WhenAll: AND of single-predicate disjunctions
    //  - WhenAny: one disjunction (OR) of predicates
    public static class ConditionBuilder
    {
        public static Condition When(Predicate predicate, bool negate = false) => WhenAll(new[] { (predicate, negate) });
        public static Condition WhenAll(params Predicate[] predicates) => WhenAll(predicates.Select(predicate => (predicate, false)));

        public static Condition WhenAll(IEnumerable<(Predicate predicate, bool negate)> predicates)
        {
            var condition = new Condition();
            foreach ((Predicate predicate, bool negate) in predicates)
            {
                var disjunction = new Condition.Disjunction();
                disjunction.AddPredicateWrapper(new Condition.PredicateWrapper(predicate, negate));
                condition.AddDisjunction(disjunction);
            }
            return condition;
        }

        public static Condition WhenAny(params Predicate[] predicates)
        {
            var disjunction = new Condition.Disjunction();
            foreach (Predicate predicate in predicates) { disjunction.AddPredicateWrapper(new Condition.PredicateWrapper(predicate)); }
            var condition = new Condition();
            condition.AddDisjunction(disjunction);
            return condition;
        }
    }
}
