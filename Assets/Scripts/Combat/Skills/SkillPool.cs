using System.Collections.Generic;
using UnityEngine;
using LowDefMustard.Utils;

namespace Frankie.Combat
{
    [CreateAssetMenu(fileName = "New Skill Pool", menuName = "Combat/Skills/New Skill Pool", order = 5)]
    public sealed class SkillPool : SkillSet
    {
        // Tunables
        [SerializeField] private List<SkillProbabilityPair> skillProbabilityPairs = new();

        public override IEnumerable<IObjectProbabilityPair<Skill>> GetWeightedSkills() => skillProbabilityPairs;
    }
}
