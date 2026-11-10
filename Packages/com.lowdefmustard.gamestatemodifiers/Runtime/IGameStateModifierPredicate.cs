using System.Collections.Generic;

namespace LowDefMustard.GameStateModifiers
{
    public interface IGameStateModifierPredicate
    {
        IEnumerable<GameStateModifier> GetGameStateModifiers();
    }
}
