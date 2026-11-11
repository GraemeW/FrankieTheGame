using System.Collections.Generic;
using LowDefMustard.Utils;

namespace LowDefMustard.GameStateModifiers.Tests
{
    public sealed class TestModifierPredicate : Predicate, IGameStateModifierPredicate
    {
        public List<GameStateModifier> modifiersToReturn = new();
        public bool returnNull;

        public IEnumerable<GameStateModifier> GetGameStateModifiers() => returnNull ? null : modifiersToReturn;
    }
}
