using UnityEngine;

namespace Frankie.Stats
{
    [CreateAssetMenu(fileName = "New Is Party Leader Predicate", menuName = "Predicates/BaseStats/Is Party Leader", order = 5)]
    public class IsPartyLeaderPredicate : PredicateBaseStats
    {
        public override bool? Evaluate(BaseStats baseStats)
        {
            if (baseStats == null) { return null; }
            return baseStats.IsInParty(out Party party) && party.IsPartyLeader(baseStats);
        }
    }
}
