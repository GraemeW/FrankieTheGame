using UnityEngine;
using Frankie.Stats;

namespace Frankie.Core.Predicates
{
    [CreateAssetMenu(fileName = "New Character Stat Exceeds Value Predicate", menuName = "Predicates/BaseStats/Character Stat Exceeds Value", order = 5)]
    public class CharacterStatExceedsValuePredicate : PredicateBaseStats
    {
        [SerializeField][Tooltip("Optional, depending on implementation")] protected Stat stat;
        [SerializeField][Tooltip("Optional, depending on implementation")] protected float value = 0f;

        public override bool? Evaluate(BaseStats baseStats)
        {
            if (character == null || baseStats == null) { return null; }
            if (CharacterProperties.AreCharacterPropertiesMatched(character, baseStats.GetCharacterProperties()))
            {
                return baseStats.GetStat(stat) >= value;
            }
            return null;
        }
    }
}
