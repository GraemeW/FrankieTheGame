using System;
using UnityEngine;
using LowDefMustard.Utils;

namespace Frankie.Combat
{
    [Serializable]
    public sealed class SkillProbabilityPair : IObjectProbabilityPair<Skill>
    {
        [SerializeField][SkillReference] private string skillReference; // Skill name ~ resolved through the Skill addressables cache (same as SkillBranch)
        [SerializeField][ProbabilityWeight(isEmptyValueSkipped: true)] private int probabilityWeight;

        // Editor Field References
        internal const string skillReferenceField = nameof(skillReference);
        internal const string probabilityWeightField = nameof(probabilityWeight);

        public SkillProbabilityPair(Skill skill, int probabilityWeight)
        {
            skillReference = skill != null ? skill.name : string.Empty;
            this.probabilityWeight = probabilityWeight;
        }

        public Skill value => Skill.GetSkillFromName(skillReference);
        public int weight => probabilityWeight;

        // New list entries come in blank (Unity zero-fills them, field initializers never run) ~ no skill + weight 0 should never be a deliberate entry
        internal void ApplyNewEntryDefaults() { if (string.IsNullOrWhiteSpace(skillReference) && probabilityWeight == 0) { probabilityWeight = 1; } }
    }
}
