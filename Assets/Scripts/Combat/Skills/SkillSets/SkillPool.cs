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

        // Editor Field References
        internal const string skillProbabilityPairsField = nameof(skillProbabilityPairs);

        public override IEnumerable<IObjectProbabilityPair<Skill>> GetWeightedSkills() => skillProbabilityPairs;

        #region UnityMethods
        private void OnValidate()
        {
            foreach (SkillProbabilityPair skillProbabilityPair in skillProbabilityPairs) { skillProbabilityPair?.ApplyNewEntryDefaults(); } // e.g. the inspector's add button ~ ensure new entries start at weight 1
        }
        #endregion
    }
}
