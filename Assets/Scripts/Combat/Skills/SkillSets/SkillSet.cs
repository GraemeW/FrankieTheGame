using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using LowDefMustard.Utils;

namespace Frankie.Combat
{
    public abstract class SkillSet : ScriptableObject
    {
        public abstract IEnumerable<IObjectProbabilityPair<Skill>> GetWeightedSkills();
        
        public IEnumerable<IObjectProbabilityPair<Skill>> GetWeightedSkills(ICollection<Skill> skillsToExclude, ICollection<Skill> skillsToChooseFrom, float? availableAP)
        {
            // Note: Non-positive weights are left in, the roll itself drops them
            return GetWeightedSkills().Where(pair =>
            {
                if (pair == null) { return false; }
                Skill skill = pair.value;
                if (skill == null) { return false; }
                if (skillsToExclude != null && skillsToExclude.Contains(skill)) { return false; }
                if (skillsToChooseFrom != null && !skillsToChooseFrom.Contains(skill)) { return false; }
                return availableAP == null || availableAP.Value >= skill.GetAPCost();
            });
        }
    }
}
