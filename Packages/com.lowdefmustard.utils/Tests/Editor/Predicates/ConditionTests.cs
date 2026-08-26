using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace LowDefMustard.Utils.Tests.Editor
{
    public class ConditionTests
    {
        // Concrete stand-in - Predicate itself is abstract with no members to implement
        private class TestPredicate : Predicate
        {
        }

        // Always returns a fixed result regardless of which predicate is asked about
        private class FixedResultEvaluator : IPredicateEvaluator
        {
            private readonly bool? result;
            public FixedResultEvaluator(bool? result) => this.result = result;
            public bool? Evaluate(Predicate predicate) => result;
        }

        // Answers differently depending on which predicate it's asked about - needed anywhere a test wants two wrappers to get different outcomes
        private class PerPredicateEvaluator : IPredicateEvaluator
        {
            private readonly Dictionary<Predicate, bool?> results;
            public PerPredicateEvaluator(Dictionary<Predicate, bool?> results) => this.results = results;
            public bool? Evaluate(Predicate predicate) => results.GetValueOrDefault(predicate);
        }

        private TestPredicate predicateA;
        private TestPredicate predicateB;

        [SetUp]
        public void SetUp()
        {
            predicateA = ScriptableObject.CreateInstance<TestPredicate>();
            predicateB = ScriptableObject.CreateInstance<TestPredicate>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(predicateA);
            Object.DestroyImmediate(predicateB);
        }

        [Test]
        public void Check_NoDisjunctions_ReturnsTrue()
        {
            // AND over an empty list is vacuously true
            var condition = new Condition();

            Assert.IsTrue(condition.Check(new List<IPredicateEvaluator>()));
        }

        [Test]
        public void Check_WrapperWithNullPredicate_AlwaysPasses()
        {
            var disjunction = new Condition.Disjunction();
            disjunction.AddPredicateWrapper(new Condition.PredicateWrapper(predicate: null));
            var condition = new Condition();
            condition.AddDisjunction(disjunction);

            var evaluators = new List<IPredicateEvaluator> { new FixedResultEvaluator(false) };

            Assert.IsTrue(condition.Check(evaluators));
        }

        [Test]
        public void Check_SingleEvaluatorAgrees_NotNegated_ReturnsTrue()
        {
            var condition = BuildSingleWrapperCondition(predicateA, negate: false);
            var evaluators = new List<IPredicateEvaluator> { new FixedResultEvaluator(true) };

            Assert.IsTrue(condition.Check(evaluators));
        }

        [Test]
        public void Check_SingleEvaluatorDisagrees_NotNegated_ReturnsFalse()
        {
            var condition = BuildSingleWrapperCondition(predicateA, negate: false);
            var evaluators = new List<IPredicateEvaluator> { new FixedResultEvaluator(false) };

            Assert.IsFalse(condition.Check(evaluators));
        }

        [Test]
        public void Check_Negated_InvertsExpectedResult()
        {
            var condition = BuildSingleWrapperCondition(predicateA, negate: true);
            var evaluators = new List<IPredicateEvaluator> { new FixedResultEvaluator(false) };

            // negate=true flips the expectation - an evaluator saying "false" now passes
            Assert.IsTrue(condition.Check(evaluators));
        }

        [Test]
        public void Check_NullResultEvaluator_AbstainsRatherThanVetoes()
        {
            // A null result never equals negate (true or false), so it can never fail the check on its own - this models "no opinion" rather than "no"
            var condition = BuildSingleWrapperCondition(predicateA, negate: false);
            var evaluators = new List<IPredicateEvaluator> { new FixedResultEvaluator(null) };

            Assert.IsTrue(condition.Check(evaluators));
        }

        [Test]
        public void Check_MultipleEvaluators_AnyDissentVetoes()
        {
            var condition = BuildSingleWrapperCondition(predicateA, negate: false);
            var evaluators = new List<IPredicateEvaluator>
            {
                new FixedResultEvaluator(true),
                new FixedResultEvaluator(false), // one dissenting evaluator is enough to fail it
                new FixedResultEvaluator(true)
            };

            Assert.IsFalse(condition.Check(evaluators));
        }

        [Test]
        public void Check_Disjunction_AnyPassingWrapperIsEnough()
        {
            var disjunction = new Condition.Disjunction();
            disjunction.AddPredicateWrapper(new Condition.PredicateWrapper(predicateA));
            disjunction.AddPredicateWrapper(new Condition.PredicateWrapper(predicateB));
            var condition = new Condition();
            condition.AddDisjunction(disjunction);

            var predicateAFailsBPasses = new PerPredicateEvaluator(new Dictionary<Predicate, bool?>
            {
                { predicateA, false },
                { predicateB, true }
            });

            // OR semantics: predicateA's wrapper fails, but predicateB's wrapper passes - one passing wrapper is enough for the disjunction to pass
            Assert.IsTrue(condition.Check(new List<IPredicateEvaluator> { predicateAFailsBPasses }));

            var bothFail = new PerPredicateEvaluator(new Dictionary<Predicate, bool?>
            {
                { predicateA, false },
                { predicateB, false }
            });

            Assert.IsFalse(condition.Check(new List<IPredicateEvaluator> { bothFail }));
        }

        [Test]
        public void Check_MultipleDisjunctions_AllMustPass()
        {
            var passingDisjunction = new Condition.Disjunction();
            passingDisjunction.AddPredicateWrapper(new Condition.PredicateWrapper(predicateA));
            var failingDisjunction = new Condition.Disjunction();
            failingDisjunction.AddPredicateWrapper(new Condition.PredicateWrapper(predicateB));

            var condition = new Condition();
            condition.AddDisjunction(passingDisjunction);
            condition.AddDisjunction(failingDisjunction);

            // A single evaluator that returns true only matters if it's asked - here it  answers true for every predicate
            // --> passingDisjunction passes and failingDisjunction also passes, making the AND as a whole pass
            var alwaysTrueEvaluator = new FixedResultEvaluator(true);
            Assert.IsTrue(condition.Check(new List<IPredicateEvaluator> { alwaysTrueEvaluator }));

            // AND semantics: a single failing disjunction fails the whole condition
            var alwaysFalseEvaluator = new FixedResultEvaluator(false);
            Assert.IsFalse(condition.Check(new List<IPredicateEvaluator> { alwaysFalseEvaluator }));
        }

        [Test]
        public void GetDisjunctions_And_GetPredicateWrappers_ReflectAddedEntries()
        {
            var disjunction = new Condition.Disjunction();
            var wrapper = new Condition.PredicateWrapper(predicateA, negate: true);
            disjunction.AddPredicateWrapper(wrapper);
            var condition = new Condition();
            condition.AddDisjunction(disjunction);

            Assert.AreEqual(1, condition.GetDisjunctions().Count);
            Assert.AreEqual(1, disjunction.GetPredicateWrappers().Count);
            Assert.AreSame(predicateA, wrapper.GetPredicate());
            Assert.IsTrue(wrapper.GetNegate());
        }

        private static Condition BuildSingleWrapperCondition(Predicate predicate, bool negate)
        {
            var disjunction = new Condition.Disjunction();
            disjunction.AddPredicateWrapper(new Condition.PredicateWrapper(predicate, negate));
            var condition = new Condition();
            condition.AddDisjunction(disjunction);
            return condition;
        }
    }
}
