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
            if (baseStats == null) { return null; }
            return baseStats.GetStat(stat) >= value;
        }
    }
}
