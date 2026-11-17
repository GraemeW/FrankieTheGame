using System;
using UnityEngine;
using LowDefMustard.Utils;

namespace Frankie.Combat
{
    [Serializable]
    public sealed class SkillProbabilityPair : IObjectProbabilityPair<Skill>
    {
        [SerializeField] private Skill skill;
        [SerializeField][Min(0)] private int probabilityWeight;

        // Editor Field References
        internal const string skillField = nameof(skill);
        internal const string probabilityWeightField = nameof(probabilityWeight);

        public SkillProbabilityPair(Skill skill, int probabilityWeight)
        {
            this.skill = skill;
            this.probabilityWeight = probabilityWeight;
        }

        public Skill value => skill;
        public int weight => probabilityWeight;
    }
}
