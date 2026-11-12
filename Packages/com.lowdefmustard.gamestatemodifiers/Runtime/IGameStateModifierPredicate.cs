using System.Collections.Generic;
using System.Linq;
using LowDefMustard.Utils;

namespace LowDefMustard.GameStateModifiers
{
    public interface IGameStateModifierPredicate
    {
        IEnumerable<GameStateModifier> GetGameStateModifiers();
        
        public static IList<GameStateModifier> CollectGameStateModifiers(IEnumerable<Predicate> predicates)
        {
            if (predicates == null) { return new List<GameStateModifier>(); }
            return predicates
                .OfType<IGameStateModifierPredicate>()
                .SelectMany(predicate => predicate.GetGameStateModifiers() ?? Enumerable.Empty<GameStateModifier>())
                .Where(gameStateModifier => gameStateModifier != null)
                .Distinct()
                .ToList();
        }
    }
}
