using UnityEngine;
using Frankie.Stats;

namespace Frankie.Core.Predicates
{
    [CreateAssetMenu(fileName = "New Is Character Predicate", menuName = "Predicates/BaseStats/Is This Character", order = 5)]
    public class IsCharacterPredicate : PredicateBaseStats
    {
        [SerializeField] protected CharacterProperties character;
        
        public override bool? Evaluate(BaseStats baseStats)
        {
            if (character == null || baseStats == null) { return null; }
            return CharacterProperties.AreCharacterPropertiesMatched(character, baseStats.GetCharacterProperties());
        }
    }
}
