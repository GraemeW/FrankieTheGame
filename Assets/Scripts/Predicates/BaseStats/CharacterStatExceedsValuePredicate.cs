using UnityEngine;
using Frankie.Stats;

namespace Frankie.Core
{
    [CreateAssetMenu(fileName = "New Character Stat Exceeds Value Predicate", menuName = "Predicates/BaseStats/Character Stat Exceeds Value")]
    public class CharacterStatExceedsValuePredicate : PredicateBaseStats
    {
        [SerializeField][Tooltip("Optional, depending on implementation")] protected Stat stat;
        [SerializeField][Tooltip("Optional, depending on implementation")] protected float value = 0f;

        public override bool? Evaluate(BaseStats baseStats)
        {
            if (character == null || baseStats == null) { return null; }
            CharacterProperties characterProperties = baseStats.GetCharacterProperties();
            if (character.GetCharacterNameID() == characterProperties.GetCharacterNameID())
            {
                return baseStats.GetStat(stat) >= value;
            }
            return null;
        }
    }
}
