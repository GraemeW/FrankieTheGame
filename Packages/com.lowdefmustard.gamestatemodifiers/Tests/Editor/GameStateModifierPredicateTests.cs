using System.Collections.Generic;
using NUnit.Framework;
using LowDefMustard.Utils;
using LowDefMustard.Utils.Tests;

namespace LowDefMustard.GameStateModifiers.Tests.Editor
{
    public class GameStateModifierPredicateTests
    {
        // Covers IGameStateModifierPredicate.CollectGameStateModifiers

        // State
        private readonly TestObjectTracker tracker = new();

        #region Setup
        [TearDown]
        public void TearDown() => tracker.DestroyAll();
        #endregion

        #region Tests
        [Test]
        public void CollectGameStateModifiers_NullPredicates_ReturnsEmpty()
        {
            CollectionAssert.IsEmpty(IGameStateModifierPredicate.CollectGameStateModifiers(null));
        }

        [Test]
        public void CollectGameStateModifiers_MixedPredicates_ReturnsEachModifierOnce()
        {
            var first = tracker.Create<TestGameStateModifier>("First");
            var second = tracker.Create<TestGameStateModifier>("Second");
            var firstPredicate = tracker.Create<TestModifierPredicate>("FirstPredicate");
            firstPredicate.modifiersToReturn.AddRange(new GameStateModifier[] { first, null, second });
            var secondPredicate = tracker.Create<TestModifierPredicate>("SecondPredicate");
            secondPredicate.modifiersToReturn.Add(first);

            IList<GameStateModifier> modifiers = IGameStateModifierPredicate.CollectGameStateModifiers(new Predicate[] { firstPredicate, tracker.Create<TestPredicate>("Plain"), null, secondPredicate });

            CollectionAssert.AreEqual(new GameStateModifier[] { first, second }, modifiers);
        }

        [Test]
        public void CollectGameStateModifiers_PredicateReturnsNull_IsSkipped()
        {
            var predicate = tracker.Create<TestModifierPredicate>("NullReturning");
            predicate.returnNull = true;

            CollectionAssert.IsEmpty(IGameStateModifierPredicate.CollectGameStateModifiers(new Predicate[] { predicate }));
        }
        #endregion
    }
}
