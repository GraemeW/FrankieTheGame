using UnityEngine;

namespace Frankie.Stats
{
    [CreateAssetMenu(fileName = "New Is Character Predicate", menuName = "Predicates/BaseStats/Is This Character", order = 9)]
    public class IsCharacterPredicate : PredicateBaseStats
    {
        // Tunables
        [SerializeField] protected CharacterProperties character;
        
        // Editor Field References
        internal const string characterField = nameof(character);
        
        // Public Methods
        public override bool? Evaluate(BaseStats baseStats)
        {
            if (character == null || baseStats == null) { return null; }
            return CharacterProperties.AreCharacterPropertiesMatched(character, baseStats.GetCharacterProperties());
        }
    }
}
