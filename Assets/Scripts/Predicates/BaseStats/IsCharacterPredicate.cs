using UnityEngine;
using Frankie.Stats;

namespace Frankie.Core
{
    [CreateAssetMenu(fileName = "New Is Character Predicate", menuName = "Predicates/BaseStats/Is Character")]
    public class IsCharacterPredicate : PredicateBaseStats
    {
        public override bool? Evaluate(BaseStats baseStats)
        {
            if (character == null || baseStats == null) { return null; }
            CharacterProperties characterProperties = baseStats.GetCharacterProperties();
            return (character.GetCharacterNameID() == characterProperties.GetCharacterNameID());
        }
    }
}
