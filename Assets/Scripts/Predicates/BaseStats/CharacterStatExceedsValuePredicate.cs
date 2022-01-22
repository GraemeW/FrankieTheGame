using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frankie.Stats;

namespace Frankie.Core
{
    [CreateAssetMenu(fileName = "New Character Stat Exceeds Value Predicate", menuName = "Predicates/BaseStats/Character Stat Exceeds Value")]
    public class CharacterStatExceedsValuePredicate : PredicateBaseStats
    {
        [SerializeField] [Tooltip("Optional, depending on implementation")] protected Stat stat = default;
        [SerializeField] [Tooltip("Optional, depending on implementation")] protected float value = 0f;

        public override bool? Evaluate(BaseStats baseStats)
        {
            if (character == null) { return null; }
            CharacterProperties characterProperties = baseStats.GetCharacterProperties();
            if (character == characterProperties)
            {
                if (baseStats.GetStat(stat) >= value)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            return null;
        }
    }
}